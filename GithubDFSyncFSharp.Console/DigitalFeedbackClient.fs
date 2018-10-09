module DigitalFeedbackClient

open System
open FSharp.Data
open Types
open Newtonsoft.Json

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

let private delete endpoint = 
    Http.RequestString
        ( DfUrl + endpoint, httpMethod = "DELETE",
        headers = [ "Authorization: ", "Bearer " + Environment.GetEnvironmentVariable("accesstoken", EnvironmentVariableTarget.Process) ])
 
let private put endpoint body= 
    Http.RequestString
      ( DfUrl + endpoint, httpMethod = "PUT",
        headers = [ "Content-Type", "application/json" ],
        body = TextRequest body)

let private getList entityType = 
    get entityType
    |> ListEntitiesPayload.Parse

let private getEntityByName entityType name= 
    let entities = getList entityType
    let entity = entities 
                    |> Array.find (fun x -> x.Name = name)
    get (entityType + "/" + entity.Id.ToString())


let private deleteHtmlCss entityType entityName =
    let scenarioToDelete = 
        getEntityByName entityType entityName
        |> HtmlCssEntityPayload.Parse
    delete ("scenarios" + "/" + scenarioToDelete.Id.ToString())

let deleteScenario scenario =
    let scenarioToDelete = 
        getEntityByName "scenarios" scenario.Name
        |> ScenarioEntityPayload.Parse
    delete ("scenarios" + "/" + scenarioToDelete.Id.ToString())

let createScenario scenario = 
    put ("scenarios") (JsonConvert.SerializeObject scenario)

let deleteInvite (invite:HtmlCssEntity) =
    deleteHtmlCss "invites" invite.Name

let deleteOverlay (overlay:HtmlCssEntity) =
    deleteHtmlCss "overlays" overlay.Name

