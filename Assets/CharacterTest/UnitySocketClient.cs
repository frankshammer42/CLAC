using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;
using CharacterTest;
using UnityEngine.Serialization;
using Object = System.Object;
using UnityEngine.UI;
using Random = System.Random;
using UnityEngine.SceneManagement;


public class UnitySocketClient : MonoBehaviour{
    private SocketManager _manager;
    private Socket _unityClientSocket;
    public string connectionAddress = "https://elsland.herokuapp.com/socket.io/" ;
    public GameObject CharacterParticleFlock;
    private string _sequence = "无生无灭无去无来无往无住";
    private int _currentSequenceProgress = 0;
    //Intro Scene Variables

    void Awake(){
        _manager = new SocketManager(new Uri(connectionAddress));
        _unityClientSocket = _manager.GetSocket("/unity-client");
        DontDestroyOnLoad(this.gameObject);
    }

    void Start(){
        _unityClientSocket.On("connect", OnConnected);
        _unityClientSocket.On("unity_character_in", OnCharacterIn);
        _unityClientSocket.On("unity_sentence_in", OnCreateSentence);
    }
    // Update is called once per frame
    void Update(){

    }
    
    //Intro Scene Functions---------------------------------------------------------------------------------------------------------------
    void OnConnected(Socket socket, Packet packet, params object[] args){
        Debug.Log("Connected to the server");
    }

    void OnCharacterIn(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("Client wrote a character");
        CharacterParticleFlock.GetComponent<ParticleFlock>().useAffector = 0;
        String character = args[0].ToString();
        Debug.Log(character);
        CharacterParticleFlock.GetComponent<ParticleFlock>().RetrieveData(character);
        CharacterParticleFlock.GetComponent<ParticleFlock>().changeRotationSpeed = false;
//        CharacterParticleFlock.GetComponent<ParticleFlock>().useAffector = 1;
        CharacterParticleFlock.GetComponent<ParticleFlock>().RotationSpeed = 1.1f;
        StartCoroutine(WaitToStartChangeSpeed());
    }

    void OnCreateSentence(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("Start Seqence");
        StartCoroutine(DrawSequence());
    }

    IEnumerator DrawSequence()
    {
        for (int i = 0; i < _sequence.Length; i++)
        {
            yield return new WaitForSeconds(12f);
            CharacterParticleFlock.GetComponent<ParticleFlock>().RetrieveData((_sequence[i]).ToString());
            //CharacterParticleFlock.GetComponent<ParticleFlock>().changeRotationSpeed = false;
//        CharacterParticleFlock.GetComponent<ParticleFlock>().useAffector = 1;
            CharacterParticleFlock.GetComponent<ParticleFlock>().RotationSpeed = 1.1f;
        }
    }
    
    

    IEnumerator WaitToStartChangeSpeed()
    {
        yield return new WaitForSeconds(15);
        CharacterParticleFlock.GetComponent<ParticleFlock>().changeRotationSpeed = true;
        //CharacterParticleFlock.GetComponent<ParticleFlock>().useAffector = 0;
    } 
    
    
}
