using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


using TMPro;

public class ChatManager : MonoBehaviour
{
    private TextMeshProUGUI chat_text;
    private TMP_InputField chat_input;
    public GameObject chat_object;
    public GameObject chat_input_object;
    public Dictionary<ulong, string> clientResponses = new Dictionary<ulong, string>();
    private int countdownDuration = 10;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Return))
        {
            chat_text = chat_object.GetComponent<TextMeshProUGUI>();
            chat_input = chat_input_object.GetComponent<TMP_InputField>();
            if (chat_input.text != "")
            {
                chat_text.text = chat_input.text;
            }
        }
    }

    [ClientRpc]
    public void MessageFromHostClientRpc()
    {
        Debug.Log("Received message from host.");
        StartCoroutine(DelayedResponseToHost());
    }
    private IEnumerator DelayedResponseToHost()
    {
        yield return new WaitForSeconds(3); // Clients respond after 3 seconds
        RespondToHostServerRpc("This is my response.");
    }
    [ServerRpc]
    public void RespondToHostServerRpc(string response)
    {
        // This code runs on the host when a client responds
        Debug.Log("Received response from a client.");
        var clientId = NetworkManager.Singleton.LocalClientId;
        // Save the client’s response in the dictionary
        if (!clientResponses.ContainsKey(clientId))
        {
            clientResponses[clientId] = response;
        }
    }
    // Call this method to start the countdown on the host
    public void StartCountdown()
    {
        StartCoroutine(CountdownCoroutine());
    }
    private IEnumerator CountdownCoroutine()
    {
        yield return new WaitForSeconds(countdownDuration);
        // Once the countdown has finished, check for clients who did not respond
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var clientId = client.Key;
            if (!clientResponses.ContainsKey(clientId))
            {
                // This client did not respond, so use the default message
                clientResponses[clientId] = "default message";
            }
        }
        // At this point, clientResponses contains a response for every client, with the default message used for any client who did not respond
    }
}
