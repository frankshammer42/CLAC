using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using BestHTTP;
using CharacterTest;



public class RAWInkData
{
    public string message;
    public InkData data;
}


public class CharacterDataRetrieverTest : MonoBehaviour
{
    // Start is called before the first frame update
    private string requestPath = "https://poetrycloud.herokuapp.com/api/characters/id/1"; 
    
    void Start()
    {
        HTTPRequest request = new HTTPRequest(new Uri(requestPath), OnRequestFinished);
        request.Send();
    }

    void OnRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        //Debug.Log("Reqeust Finished! Cloud Data Received: " + response.DataAsText);
        RAWInkData character = JsonUtility.FromJson<RAWInkData>(response.DataAsText);
        Debug.Log(character.message);
        Debug.Log(character.data.name);
        Debug.Log(character.data.id);
        Debug.Log(character.data.inks.Length);
    }
}
