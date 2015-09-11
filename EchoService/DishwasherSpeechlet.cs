﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.Slu;
using AlexaSkillsKit.UI;
using Humanizer;
using RestSharp;
using Shared;

namespace EchoService
{
    public class DishwasherSpeechlet : Speechlet
    {
        // Note: NAME_KEY being a JSON property key gets camelCased during serialization
        private const string NAME_KEY = "name";
        private const string NAME_SLOT = "Name";


        public override void OnSessionStarted(SessionStartedRequest request, Session session)
        {
            Console.WriteLine("OnSessionStarted requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
        }


        public override SpeechletResponse OnLaunch(LaunchRequest request, Session session)
        {
            Console.WriteLine("OnLaunch requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
            return GetWelcomeResponse();
        }


        public override SpeechletResponse OnIntent(IntentRequest request, Session session)
        {
            Console.WriteLine("OnIntent requestId={0}, sessionId={1}", request.RequestId, session.SessionId);

            // Get intent from the request object.
            Intent intent = request.Intent;
            string intentName = intent?.Name;

            // Note: If the session is started with an intent, no welcome message will be rendered;
            // rather, the intent specific response will be returned.
            if ("StatusIntent".Equals(intentName))
            {
                return ShareStatus(intent, session);
            }
            else if ("SpecificStatusQueryIntent".Equals(intentName))
            {
                return CheckRequestedStatus(intent, session);
            }
            else
            {
                throw new SpeechletException("Invalid Intent");
            }
        }


        public override void OnSessionEnded(SessionEndedRequest request, Session session)
        {
            Console.WriteLine("OnSessionEnded requestId={0}, sessionId={1}", request.RequestId, session.SessionId);
        }


        /**
         * Creates and returns a {@code SpeechletResponse} with a welcome message.
         * 
         * @return SpeechletResponse spoken and visual welcome message
         */
        private SpeechletResponse GetWelcomeResponse()
        {
            // Create the welcome message.
            string speechOutput =
                "Welcome to the Alexa AppKit session sample app, please tell me your name by saying, my name is Sam";

            // Here we are setting shouldEndSession to false to not end the session and
            // prompt the user for input
            return BuildSpeechletResponse("Welcome", speechOutput, false);
        }


        /**
         * Creates a {@code SpeechletResponse} for the intent and stores the extracted name in the
         * Session.
         * 
         * @param intent
         *            intent for the request
         * @return SpeechletResponse spoken and visual response the given intent
         */
        private SpeechletResponse SetNameInSessionAndSayHello(Intent intent, Session session)
        {
            // Get the slots from the intent.
            Dictionary<string, Slot> slots = intent.Slots;

            // Get the name slot from the list slots.
            Slot nameSlot = slots[NAME_SLOT];
            string speechOutput = "";

            // Check for name and create output to user.
            if (nameSlot != null)
            {
                // Store the user's name in the Session and create response.
                string name = nameSlot.Value;
                session.Attributes[NAME_KEY] = name;
                speechOutput = String.Format(
                    "Hello {0}, now I can remember your name, you can ask me your name by saying, whats my name?", name);
            }
            else
            {
                // Render an error since we don't know what the users name is.
                speechOutput = "I'm not sure what your name is, please try again";
            }

            // Here we are setting shouldEndSession to false to not end the session and
            // prompt the user for input
            return BuildSpeechletResponse(intent.Name, speechOutput, false);
        }

        private SpeechletResponse CheckRequestedStatus(Intent intent, Session session)
        {
            Dictionary<string, Slot> slots = intent.Slots;

            Slot statusSlot = slots["status"];
            string speechOutput = "";

            if (statusSlot != null)
            {
                var statusResponse = GetStatusResponse();

                var status = Enum.Parse(typeof(DishwasherStatus), statusResponse.Status) is DishwasherStatus ? (DishwasherStatus)Enum.Parse(typeof(DishwasherStatus), statusResponse.Status) : DishwasherStatus.Clean;

                if (statusSlot.Value == "running")
                {
                    if (status == DishwasherStatus.Running)
                    {
                        var runningStatusDetails = statusResponse.Details as StatusResponse.RunningStatusDetails;

                        var runningTimeSpan = DateTime.Now - runningStatusDetails.StartTime;
                        var estimatedTimeLeft = runningStatusDetails.EstimatedEndTime - DateTime.Now;

                        speechOutput =
                            $"Actually I am, and I have been for {runningTimeSpan.Humanize()}.  I expect to be clean in {estimatedTimeLeft.Humanize()}";

                        // It would be cool if we could then set a reminder for you to empty it at the estimated time here
                    }
                    else
                    {
                        speechOutput = $"No, I am not running.  I am {status.ToString()}";
                    }
                }
                else if (statusSlot.Value == "clean")
                {
                    if (status == DishwasherStatus.Clean)
                    {
                        var cleanStatusDetails = statusResponse.Details as StatusResponse.CleanStatusDetails;

                        var cleanTimeSpan = DateTime.Now - cleanStatusDetails.DishwasherRun.EndDateTime;

                        speechOutput =
                            $"Please empty me!  I have been clean for  {cleanTimeSpan.Humanize()}.";
                    }
                    else
                    {
                        speechOutput = $"No, I am not clean.  I am {status.ToString()}";
                    }
                }
                else if (statusSlot.Value == "dirty")
                {
                    if (status == DishwasherStatus.Dirty)
                    {
                        var dirtyStatusDetails = statusResponse.Details as StatusResponse.DirtyStatusDetails;

                        var dirtyTimeSpan = DateTime.Now - dirtyStatusDetails.DirtyTime;

                        speechOutput =
                            $"Yes, I am dirty.";
                    }
                    else
                    {
                        speechOutput = $"No, I am not dirty.  I am {status.ToString()}";
                    }
                }
            }
            else
            {
                speechOutput = "I'm not sure what you are asking me, please try again";
            }

            return BuildSpeechletResponse(intent.Name, speechOutput, true);
        }

        private SpeechletResponse ShareStatus(Intent intent, Session session)
        {
            var statusResponse = GetStatusResponse();

            var speechOutput = string.Empty;

            if (statusResponse != null)
            {
                var status = Enum.Parse(typeof (DishwasherStatus), statusResponse.Status) is DishwasherStatus ? (DishwasherStatus) Enum.Parse(typeof (DishwasherStatus), statusResponse.Status) : DishwasherStatus.Clean;

                switch (status)
                {
                    case DishwasherStatus.Clean:
                        var cleanStatusDetails = statusResponse.Details as StatusResponse.CleanStatusDetails;

                        var cleanTimeSpan = DateTime.Now - cleanStatusDetails.DishwasherRun.EndDateTime;
                        
                        speechOutput =
                            $"I am clean and have been for {cleanTimeSpan.Humanize()}, please get somebody to empty me";
                        break;
                    case DishwasherStatus.Running:
                        var runningStatusDetails = statusResponse.Details as StatusResponse.RunningStatusDetails;

                        var runningTimeSpan = DateTime.Now - runningStatusDetails.StartTime;
                        speechOutput = $"I have been running for {runningTimeSpan.Humanize()}.";
                        break;
                    case DishwasherStatus.Dirty:
                        var dirtyStatusDetails = statusResponse.Details as StatusResponse.DirtyStatusDetails;

                        var dirtyTimeSpan = DateTime.Now - dirtyStatusDetails.DirtyTime;
                        speechOutput = $"I have been dirty for {dirtyTimeSpan.Humanize()}.";
                        break;
                    default:
                        speechOutput = "I have no idea";
                        break;
                }
            }

            return BuildSpeechletResponse(intent.Name, speechOutput, true);
        }

        private static StatusResponse GetStatusResponse()
        {
            var restResponse = new RestClient("http://10.170.142.9/api/status").Get<StatusResponse>(new RestRequest());

            var statusResponse = restResponse.Data;
            return statusResponse;
        }

        /**
         * Creates a {@code SpeechletResponse} for the intent and get the user's name from the Session.
         * 
         * @param intent
         *            intent for the request
         * @return SpeechletResponse spoken and visual response for the intent
         */
        private SpeechletResponse GetNameFromSessionAndSayHello(Intent intent, Session session)
        {
            string speechOutput = "";
            bool shouldEndSession = false;

            // Get the user's name from the session.
            string name = (String)session.Attributes[NAME_KEY];

            // Check to make sure user's name is set in the session.
            if (!String.IsNullOrEmpty(name))
            {
                speechOutput = String.Format("Your name is {0}, goodbye", name);
                shouldEndSession = true;
            }
            else
            {
                // Since the user's name is not set render an error message.
                speechOutput = "I'm not sure what your name is, you can say, my name is Sam";
            }

            return BuildSpeechletResponse(intent.Name, speechOutput, shouldEndSession);
        }


        /**
         * Creates and returns the visual and spoken response with shouldEndSession flag
         * 
         * @param title
         *            title for the companion application home card
         * @param output
         *            output content for speech and companion application home card
         * @param shouldEndSession
         *            should the session be closed
         * @return SpeechletResponse spoken and visual response for the given input
         */
        private SpeechletResponse BuildSpeechletResponse(string title, string output, bool shouldEndSession)
        {
            // Create the Simple card content.
            SimpleCard card = new SimpleCard();
            card.Title = String.Format("SessionSpeechlet - {0}", title);
            card.Subtitle = String.Format("SessionSpeechlet - Sub Title");
            card.Content = String.Format("SessionSpeechlet - {0}", output);

            // Create the plain text output.
            PlainTextOutputSpeech speech = new PlainTextOutputSpeech();
            speech.Text = output;

            // Create the speechlet response.
            SpeechletResponse response = new SpeechletResponse();
            response.ShouldEndSession = shouldEndSession;
            response.OutputSpeech = speech;
            response.Card = card;
            return response;
        }
    }
}
