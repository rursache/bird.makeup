﻿using System;
using BirdsiteLive.ActivityPub.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace BirdsiteLive.ActivityPub
{
    public class ApDeserializer
    {
        public static Activity ProcessActivity(string json)
        {
            try
            {
                var activity = JsonSerializer.Deserialize<Activity>(json);
                switch (activity.type)
                {
                    case "Follow":
                        return JsonSerializer.Deserialize<ActivityFollow>(json);
                    case "Undo":
                        var a = JsonSerializer.Deserialize<ActivityUndo>(json);
                        if(a.apObject.type == "Follow")
                            return JsonSerializer.Deserialize<ActivityUndoFollow>(json);
                        break;
                    case "Delete":
                        return JsonSerializer.Deserialize<ActivityDelete>(json);
                    case "Accept":
                        var accept = JsonSerializer.Deserialize<ActivityAccept>(json);
                        switch (accept.apObject.type)
                        {
                            case "Follow":
                                var acceptFollow = new ActivityAcceptFollow()
                                {
                                    type = accept.type,
                                    id = accept.id,
                                    actor = accept.actor,
                                    context = accept.context,
                                    apObject = new ActivityFollow()
                                    {

                                        id = accept.apObject.id,
                                        type = accept.apObject.type,
                                        actor = accept.apObject.actor,
                                        context = accept.apObject.context?.ToString(),
                                        apObject = accept.apObject.apObject,
                                    }
                                };
                                return acceptFollow;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

    }
}