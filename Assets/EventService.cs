using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class EventService : MonoBehaviour
{
    [SerializeField] private string serverUrl;
    [SerializeField] private float cooldownBeforeSend = 3f;

    private List<EventInfo> eventsBuffer;
    private EventsWrapper eventsWrapper;

    private bool isCooldown = false;

    private async void Start(){
        eventsBuffer = new List<EventInfo>();
        eventsWrapper = new EventsWrapper();
        eventsWrapper.Events = eventsBuffer;

        TrackEvent("level_start", "level:3");
        TrackEvent("level_start", "level:2");
        
        Debug.Log(ParseEventsToJson());
        await SendEvents();
    }

    private async Task SendEvents(){
        string json = ParseEventsToJson();
        
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        
        while (!operation.isDone){
            await Task.Yield();
        }

        if(request.result == UnityWebRequest.Result.Success){
            Debug.Log("Success");
            // clear all the stuff
            eventsBuffer.Clear();
        }
        else{
            Debug.Log("Error");
            // handle error
        }
    }

    private async void CoolDownAndSend(){
        if(isCooldown) return;
        isCooldown = true;
        await Task.Delay((int)(cooldownBeforeSend * 1000));
        await SendEvents();
        isCooldown = false;
    }

    private string ParseEventsToJson(){

        var eventWrapper = new {
            events = eventsBuffer
        };

        string json = JsonConvert.SerializeObject(eventWrapper, Formatting.Indented);

        return json;
    }

    public void TrackEvent(string type, string data){
        eventsBuffer.Add(new EventInfo(type, data));
        CoolDownAndSend();
    }
}

public class EventsWrapper{

    private List<EventInfo> events;

    [JsonProperty("events")]
    public List<EventInfo> Events
    {
        get => events;
        set => events = value;
    }
}

public struct EventInfo
{
    private readonly string type;
    private readonly string data;

    [JsonProperty("type")]
    public string Type => type;
    [JsonProperty("data")]
    public string Data => data;

    public EventInfo(string type, string data){
        this.type = type;
        this.data = data;
    }

    public override string ToString(){
        return $"type: {type}, data: {data}";
    }
}
