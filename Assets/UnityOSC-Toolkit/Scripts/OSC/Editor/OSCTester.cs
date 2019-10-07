using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityOSC;
using System.Net;

public class OSCTester : EditorWindow
{
    public int listenPort;

    private Dictionary<string,int> messages = new Dictionary<string,int>();

    OSCServer server;
    OSCClient client;
    bool dirty = false;

    [MenuItem("Window/OSC Tester")]
    static void OpenWindow()
    {
        OSCTester window = EditorWindow.GetWindow<OSCTester>();
    }

    private void OnEnable()
    {
        messages.Clear();
    }

    private void Update()
    {
        if (dirty)
        {
            Repaint();
            dirty = false;
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    void Connect()
    {
        if (server != null)
        {
            Disconnect();
        }

        //Debug.Log("Connecting");
        server = new OSCServer(listenPort);
        server.PacketReceivedEvent += PackedReceived;
    }

    void Disconnect()
    {
        if (server != null)
        {
            //Debug.Log("Disconnecting");
            server.Close();
            server = null;
        }
    }

    void PackedReceived(OSCServer sender, OSCPacket packet)
    {
        if ( packet.IsBundle() )
        {
            foreach( OSCMessage m in packet.Data)
            {
                HandleMessage(m);
            }
        }
        else
        {
            HandleMessage(packet as OSCMessage);
        }
    }

    void HandleMessage(OSCMessage m)
    {
        if (messages.ContainsKey(m.Address))
        {
            messages[m.Address]++;
        }
        else
        {
            messages.Add(m.Address, 1);
        }

        dirty = true;
    }

    private void Start()
    {
        messages.Clear();
        Connect();
    }

    private void Stop()
    {
        Disconnect();
        client = null;
    }

    private void Test()
    {
        client = new OSCClient(IPAddress.Parse("127.0.0.1"), listenPort);
        OSCMessage m = new OSCMessage("/testMessage");
        client.Send(m);
        client.Flush();
        client.Close();
        client = null;
    }

    private void OnGUI()
    {
        listenPort = EditorGUILayout.IntField("OSC Listen Port: ", listenPort, GUILayout.Width(250));

        EditorGUILayout.BeginHorizontal();

        if ( GUILayout.Button( "Start" ))
        {
            Start();
        }

        if (GUILayout.Button("Stop"))
        {
            Stop();
        }

        //if (GUILayout.Button("Test"))
        //{
        //    Test();
        //}

        EditorGUILayout.EndHorizontal();

        if ( server == null ) {
            EditorGUILayout.LabelField("Press Start To Listen", EditorStyles.boldLabel);
        }
        else {
            EditorGUILayout.LabelField("Listening...", EditorStyles.boldLabel);
        }

        foreach ( KeyValuePair<string,int> pair in messages)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("("+pair.Value+"): "+pair.Key);
            if ( GUILayout.Button("Copy"))
            {
                var textEditor = new TextEditor();
                textEditor.text = pair.Key;
                textEditor.SelectAll();
                textEditor.Copy();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}