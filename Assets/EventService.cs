using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine.Networking;
using Cysharp.Threading;
using System.IO;
using Cysharp.Threading.Tasks;

public class EventService : MonoBehaviour
{
    [SerializeField] private string serverUrl;
    [SerializeField] private float cooldownBeforeSend = 3f;
    [SerializeField] private string filePath = "events.json";

    private List<EventInfo> eventsBuffer;
    private EventsWrapper eventsWrapper;

    private bool isCooldown = false;

    private string persistentDataPath;

    public void TrackEvent(string type, string data)
    {
        eventsBuffer.Add(new EventInfo(type, data));
        CoolDownAndSend();
    }

    private void Start(){
        persistentDataPath = Path.Combine(Application.persistentDataPath, filePath);
        LoadEventsFromFile();
        if(eventsBuffer == null){
            eventsBuffer = new List<EventInfo>();
        }
        eventsWrapper = new EventsWrapper();
        eventsWrapper.Events = eventsBuffer;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveEventsToFile();
        }
    }

    private void LoadEventsFromFile(){
        if(File.Exists(persistentDataPath)){
            string json = File.ReadAllText(persistentDataPath);
            eventsBuffer = JsonConvert.DeserializeObject<List<EventInfo>>(json);
        }
    }

    private void SaveEventsToFile(){
        string json = JsonConvert.SerializeObject(eventsBuffer, Formatting.Indented);
        try{
            File.WriteAllText(persistentDataPath, json);
        }
        catch{
            Debug.LogError("Failed to save events to file");
        }
    }

    private void OnDestroy() {
        SaveEventsToFile();
    }


    private async UniTask SendEvents(){
        string json = ParseEventsToJson();
        
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        
        while (!operation.isDone){
            await UniTask.NextFrame();
        }

        if(request.result == UnityWebRequest.Result.Success){
            eventsBuffer.Clear();
            File.Delete(persistentDataPath);
        }
        else{
            SaveEventsToFile();
        }
    }

    private async void CoolDownAndSend(){
        if(isCooldown) return;
        isCooldown = true;
        try
        {
            await UniTask.WaitForSeconds(cooldownBeforeSend);
            await SendEvents();
        }
        finally{
            isCooldown = false;
        }
    }

    private string ParseEventsToJson(){

        var eventWrapper = new {
            events = eventsBuffer
        };

        string json = JsonConvert.SerializeObject(eventWrapper, Formatting.Indented);

        return json;
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

[Serializable]
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
