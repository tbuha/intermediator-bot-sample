using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Models;
using Underscore.Bot.MessageRouting.Results;
using RoutingDataManagerS = Underscore.Bot.MessageRouting.DataStore.RoutingDataManager;
namespace Underscore.Bot.MessageRouting
{
    public static class MessageRouterExt
    {
        public static async Task<MessageRoutingResult> RouteReachMessageIfSenderIsConnectedAsync(this MessageRouter messageRouter, IMessageActivity activity, bool addNameToMessage = true)
        {
            var RoutingDataManager = messageRouter.RoutingDataManager;
            ConversationReference sender = MessageRouter.CreateSenderConversationReference(activity);
            Connection connection = RoutingDataManager.FindConnection(sender);

            MessageRoutingResult messageRoutingResult = new MessageRoutingResult()
            {
                Type = MessageRoutingResultType.NoActionTaken,
                Connection = connection
            };

            if (connection != null)
            {
                ConversationReference recipient =
                    RoutingDataManagerS.Match(sender, connection.ConversationReference1)
                        ? connection.ConversationReference2 : connection.ConversationReference1;

                if (recipient != null)
                {
                    //string message = activity.Text;

                    //if (addNameToMessage)
                    //{
                    //    string senderName = RoutingDataManager.GetChannelAccount(sender).Name;

                    //    if (!string.IsNullOrWhiteSpace(senderName))
                    //    {
                    //        message = $"{senderName}: {message}";
                    //    }
                    //}

                    var str = JsonConvert.SerializeObject(activity);
                    var newActivity = JsonConvert.DeserializeObject<Microsoft.Bot.Schema.Activity>(str);
                    newActivity.From = null;
                    if (recipient.Conversation != null)
                    {
                        newActivity.Conversation = recipient.Conversation;
                    }
                    ChannelAccount recipientChannelAccount = RoutingDataManagerS.GetChannelAccount(recipient);
                    if (recipientChannelAccount != null)
                    {
                        newActivity.Recipient = recipientChannelAccount;
                    }

                    ResourceResponse resourceResponse = await messageRouter.SendMessageAsync(recipient, newActivity);

                    if (resourceResponse != null)
                    {
                        messageRoutingResult.Type = MessageRoutingResultType.MessageRouted;

                        if (!RoutingDataManager.UpdateTimeSinceLastActivity(connection))
                        {
                            messageRouter.Logger.Log("Failed to update the time since the last activity property of the connection");
                        }
                    }
                    else
                    {
                        messageRoutingResult.Type = MessageRoutingResultType.FailedToRouteMessage;
                        messageRoutingResult.ErrorMessage = $"Failed to forward the message to the recipient";
                    }
                }
                else
                {
                    messageRoutingResult.Type = MessageRoutingResultType.Error;
                    messageRoutingResult.ErrorMessage = "Failed to find the recipient to forward the message to";
                }
            }

            return messageRoutingResult;
        }
    }
}
