using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectEvent : EventBase
{
    [SerializeField] CollectEventHomeDisplayer protoDisplayer;

    private bool winFlag;
    private int displayProgress;
    private CollectEventHomeDisplayer displayer;
    private EventData eventData;

    protected struct EventData
    {
        public int progress;
    }

    public override bool ActiveCheck()
    {
        string dataString = UserDataManager.LoadEventData(eventID);
        if (dataString != "")
            eventData = JsonConvert.DeserializeObject<EventData>(dataString);
        else
            eventData = new EventData { progress = 0 };
        return true;
    }

    public override void ShowMe(Transform parent)
    {
        displayer = Instantiate(protoDisplayer, parent);
        displayer.UpdateDisplay(eventData.progress, 500);
        winFlag = UserDataManager.WinFlag;
        if (UserDataManager.WinFlag)
        {
            if (UserDataManager.ClearedPieceDict.ContainsKey(1))
                eventData.progress += UserDataManager.ClearedPieceDict[1];
            displayProgress = eventData.progress;
            eventData.progress = eventData.progress % 500;
            var setting = new JsonSerializerSettings();
            setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            UserDataManager.SaveEventData(eventID, JsonConvert.SerializeObject(eventData, setting));
        }
    }

    public override async void OnSceneStart()
    {
        if (!winFlag)
            return;
        await displayer.CollectTween();
        while (500 <= displayProgress)
        {
            await displayer.ProgressTween(500, 500);
            displayProgress -= 500;
        }
        await displayer.ProgressTween(displayProgress, 500);
    }


    public override UniTask StartCallback()
    {
        return UniTask.CompletedTask;
    }
}