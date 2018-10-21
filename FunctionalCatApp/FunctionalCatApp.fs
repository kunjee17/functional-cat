// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.
namespace FunctionalCatApp

open System.Diagnostics
open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms
open System
open System.Net
open FSharp.Core
open System.Xml.Linq
open System.Linq
open FSharp.Data


module App = 

    let [<Literal>] catUrl = "http://thecatapi.com/api/images/get?format=xml&type=jpg"
 
    type Model = { 
        Id : string 
        SourceUrl : string
        Url :string
    }

    type Msg = 
        | Next
        | Fetched of string 
        | FetchError of exn  


    let initModel = { Id = ""; SourceUrl = ""; Url = "" }

    let fetchNewCat =
        async {
            use wc = new WebClient()
            let Uri = Uri(catUrl)
            let! data = Async.Catch (wc.DownloadStringTaskAsync(catUrl) |> Async.AwaitTask )
            match data with
            | Choice1Of2 s -> return Fetched s
            | Choice2Of2 exn -> return FetchError exn 
        } |> Cmd.ofAsyncMsg



    let init () =
        initModel, Cmd.batch [
            Cmd.none //just to give an example of batch command
            fetchNewCat
        ]

    // FSharp.Data is not working here... 
    // TODO: Make FSharp.Data work... 
    let [<Literal>] catXMl = """
    <response>
        <data>
            <images>
                <image>
                    <url>http://25.media.tumblr.com/tumblr_m2rdm4welu1qjahcpo1_1280.jpg</url>
                    <id>tl</id>
                    <source_url>http://thecatapi.com/?id=tl</source_url>
                </image>
            </images>
        </data>
    </response>
    """

    type CatProvider = XmlProvider<catXMl>


    let update msg model =
        match msg with
        | Fetched s ->
            let catDoc = CatProvider.Parse(s)
            {
                Id = catDoc.Data.Image.Id
                Url = catDoc.Data.Image.Url
                SourceUrl = catDoc.Data.Image.SourceUrl
            }, Cmd.none
        | FetchError exn -> 
            model, Cmd.none
        | Next -> model, fetchNewCat

    let view (model: Model) dispatch =
        View.ContentPage(
          content = View.StackLayout(padding = 20.0, verticalOptions = LayoutOptions.Center,
            children = [
                View.Image(source = model.Url, horizontalOptions = LayoutOptions.CenterAndExpand)
                //View.Label(text = sprintf "Id is %s" model.Id, horizontalOptions = LayoutOptions.Center)
                View.Label(text = sprintf "Source Url is %s" model.SourceUrl, horizontalOptions = LayoutOptions.Center)
                View.Button(text = "Next", horizontalOptions = LayoutOptions.Center, command = (fun _ -> Next |> dispatch) )
            ]))

    // Note, this declaration is needed if you enable LiveUpdate
    let program = Program.mkProgram init update view

type App () as app = 
    inherit Application ()

    let runner = 
        App.program
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> Program.runWithDynamicView app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Elmish.XamarinForms/tools.html for further  instructions.
    //
    // do runner.EnableLiveUpdate()
#endif    

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Elmish.XamarinForms/models.html for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif


