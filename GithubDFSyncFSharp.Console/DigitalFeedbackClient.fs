module DigitalFeedbackClient

open System
open FSharp.Data

[<Literal>]
let private DfUrl = """
https://author.testlab.firmglobal.net/digitalfeedback/api/programs/4
"""

type ListEntitiesPayload = JsonProvider<""" [{"id":1,"programId":4,"name":"sc1"}] """>
type HtmlCssEntityPayload = JsonProvider<""" {"id":2,"programId":4,"name":"o1","html":"somehtml","css":"somecss"} """>
type ScenarioEntityPayload = JsonProvider<""" {"id":3,"programId":4,"name":"sc1","script":"javascript","css":"somecss", "isEnabled":true} """>

let private get endpoint= 
    Http.RequestString
        ( DfUrl + endpoint, httpMethod = "GET",
        headers = [ "Authorization: ", "Bearer " + Environment.GetEnvironmentVariable("accesstoken", EnvironmentVariableTarget.Process) ])

let private getList entityType = 
    get entityType
    |> ListEntitiesPayload.Parse

let private getEntityByName entityType name= 
    let entities = getList entityType
    let entity = entities 
                    |> Array.find (fun x -> x.Name = name)
    get (entityType + "/" + entity.Id.ToString())
