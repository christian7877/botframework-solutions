﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BotProject.Actions.MSGraph
{
    public class GetContacts : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetContacts";

        [JsonConstructor]
        public GetContacts([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("nameProperty")]
        public string NameProperty { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var name = await new TextTemplate(NameProperty).BindToData(dc.Context, dc.GetState());
            var token = await new TextTemplate(Token).BindToData(dc.Context, dc.GetState());

            var graphClient = GraphClient.GetAuthenticatedClient(token);
            var results = new List<object>();
            var optionList = new List<QueryOption>();
            optionList.Add(new QueryOption("$search", $"\"{name}\""));

            // Get the current user's profile.
            IUserContactsCollectionPage contacts = null;
            try
            {
                contacts = await graphClient.Me.Contacts.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (contacts?.Count > 0)
            {
                foreach (var contact in contacts)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in contact.EmailAddresses)
                    {
                        emailAddresses.Add(email.Address);
                    }

                    // Get user properties.
                    results.Add(new
                    {
                        Name = contact.DisplayName,
                        EmailAddresses = emailAddresses
                    });
                }
            }

            // Get the current user's profile.
            IUserPeopleCollectionPage people = null;
            try
            {
                people = await graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (people?.Count > 0)
            {
                foreach (var person in people)
                {
                    var emailAddresses = new List<string>();

                    foreach (var email in person.ScoredEmailAddresses)
                    {
                        emailAddresses.Add(email.Address);
                    }

                    // Get user properties.
                    results.Add(new
                    {
                        Name = person.DisplayName,
                        EmailAddresses = emailAddresses
                    });
                }
            }

            var jsonResult = JToken.FromObject(results);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetContacts), jsonResult, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.GetState().SetValue(this.ResultProperty, jsonResult);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: jsonResult, cancellationToken: cancellationToken);
        }
    }
}