// Learn more about F# at http://fsharp.org

open System
open FSharp.Data
open System.IO
open System.Collections.Immutable

[<Literal>]
let pushPayloadSample = """
{
  "head_commit": {
    "added": [
        "Containers/container.css",
        "Containers/container.htm"
    ],
    "removed": [
        "Invites/invite.css",
        "Invites/invite.htm"
    ],
    "modified": [
        "Scenarios/scenario1.js"
    ]
  }
}
"""

type GitHubPushPayload = JsonProvider<pushPayloadSample>

type Operation = Add | Modify | Remove

type GitHubFile = 
    | AddedFile of string * string
    | ModifiedFile of string * string 
    | RemovedFile of string
    
type HtmlCssEntity = 
    {
        Name:string
        Html: string option
        Css: string option
    }

type Scenario =
    {
        Name:string
        Script:string option
    }

type DFEntity = 
    | Scenario of Scenario*Operation
    | Overlay of HtmlCssEntity*Operation
    | Invite of HtmlCssEntity*Operation

let downloadFileContent path =
    Http.RequestString ("https://raw.githubusercontent.com/Ten1n/DigitalFeedback/master/" + path)

let getPath gitHubFile = 
    match gitHubFile with 
    | AddedFile (path, _) -> path
    | ModifiedFile (path, _) -> path
    | RemovedFile path -> path

let getContent gitHubFile =
    match gitHubFile with 
    | AddedFile (_, content) -> content
    | ModifiedFile (_, content) -> content
    | RemovedFile _ ->  string None
    
let mapFileToOperation gitHubFile =
    match gitHubFile with 
    | AddedFile (_, _) -> Operation.Add
    | ModifiedFile (_, _) -> Operation.Modify
    | RemovedFile _ ->  Operation.Remove

let inviteMapper (name, files) =
    let html = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".htm") |> Option.map getContent
    let css = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".css") |> Option.map getContent
    Invite({Name = name; Html = html; Css = css}, files.[0] |> mapFileToOperation)

let overlayMapper (name, files) =
    let html = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".htm") |> Option.map getContent
    let css = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".css") |> Option.map getContent
    Overlay({Name = name; Html = html; Css = css}, files.[0] |> mapFileToOperation)

let scenarioMapper (name, files) =
    let js = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".js") |> Option.map getContent
    Scenario({Name = name; Script = js}, files.[0] |> mapFileToOperation)

let getDFEntitiesByType gitHubUpdateInfo entityType mapfunc=
    gitHubUpdateInfo 
        |> Array.filter (fun x -> getPath x |> System.IO.Path.GetDirectoryName |> (=) entityType)        
        |> Array.groupBy (fun x-> getPath x |> System.IO.Path.GetFileNameWithoutExtension)
        |> Array.map mapfunc

let updateDFEntity dfEntity = 
    match dfEntity with 
    | Scenario (scenario, Operation.Add) -> sprintf "Scenario %s add" scenario.Name
    | Scenario (scenario, Operation.Modify) -> sprintf "Scenario %s modify" scenario.Name
    | Scenario (scenario, Operation.Remove) -> sprintf "Scenario %s remove" scenario.Name
    | Overlay (scenario, Operation.Add) -> sprintf "Overlay %s add" scenario.Name
    | Overlay (scenario, Operation.Modify) -> sprintf "Overlay %s modify" scenario.Name
    | Overlay (scenario, Operation.Remove) -> sprintf "Overlay %s remove" scenario.Name
    | Invite (scenario, Operation.Add) -> sprintf "Invite %s add" scenario.Name
    | Invite (scenario, Operation.Modify) -> sprintf "Invite %s modify" scenario.Name
    | Invite (scenario, Operation.Remove) -> sprintf "Invite %s remove" scenario.Name
    

[<EntryPoint>]
let main argv =
    let input = IO.File.ReadAllText("input.txt")

    let pushPayload = GitHubPushPayload.Parse input

    let addedFiles = pushPayload.HeadCommit.Added |> Array.map (fun file -> AddedFile(file, downloadFileContent file))
    let modifiedFiles = pushPayload.HeadCommit.Modified |> Array.map (fun file -> ModifiedFile(file, downloadFileContent file))
    let removedFiles = pushPayload.HeadCommit.Removed |> Array.map (fun file -> RemovedFile(file))

    let gitHubUpdateInfo =  Array.concat (
                                seq {
                                    yield addedFiles
                                    yield modifiedFiles
                                    yield removedFiles
                                })
    
    let mappedInvites = 
        getDFEntitiesByType gitHubUpdateInfo "Invites" inviteMapper
    
    let mappedOverlays = 
        getDFEntitiesByType gitHubUpdateInfo "Containers" overlayMapper
    
    let mappedScenarios = 
        getDFEntitiesByType gitHubUpdateInfo "Scenarios" scenarioMapper
    
    let mappedEntities =  Array.concat (
                                seq {
                                    yield mappedInvites
                                    yield mappedOverlays
                                    yield mappedScenarios
                                })
    mappedEntities 
    |> Array.map updateDFEntity
    |> Array.map (fun x -> printfn "%s" x)
    |> ignore
    //for file in tmp do 
    //    printfn "%s" file
    0 // return an integer exit code
