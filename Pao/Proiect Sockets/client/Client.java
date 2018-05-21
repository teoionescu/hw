package client;

import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.Socket;
import java.io.Console;
import java.util.Scanner;
import javax.swing.JOptionPane;

import common.MsgType;
import common.Message;
import common.MessageFactory;

class ClientStatus {
    private static Boolean loggedIn = false;
    public synchronized static Boolean getLoggedIn() {
        return ClientStatus.loggedIn;
    }
    public synchronized static void setLoggedIn(Boolean _loggedIn) {
        ClientStatus.loggedIn = _loggedIn;
    }
}

class ClientConsole extends Thread
{
    private MessageFactory messageFactoryInstance = new MessageFactory();
    ObjectOutputStream mStreamToServer;

    public Message parseMessage(String msgText)
    {
        final String[] tokens = msgText.split(" ");
        if(tokens.length == 0) {
            return null;
        }
        if (ClientStatus.getLoggedIn() == false) {
            return messageFactoryInstance.createMessage(MsgType.MSG_HANDSHAKE, tokens[0]);
        }
        Message msg = null;
        if (tokens[0].compareTo("send") == 0 && tokens.length >= 3) {
            final String[] msgTokens = new String[tokens.length - 2];
            for (int i = 2; i < tokens.length; i++) {
                msgTokens[i - 2] = tokens[i];
            }
            msg = messageFactoryInstance.createMessage(
                MsgType.MSG_CHAT,
                tokens[1],
                String.join(" ", msgTokens)
            );
        } else if (tokens[0].compareTo("broad") == 0 && tokens.length >= 2) {
            final String[] msgTokens = new String[tokens.length - 1];
            for (int i = 1; i < tokens.length; i++) {
                msgTokens[i - 1] = tokens[i];
            }
            msg = messageFactoryInstance.createMessage(
                MsgType.MSG_BROADCAST,
                null,
                String.join(" ", msgTokens)
            );
        } else if (tokens[0].compareTo("disconnect") == 0) {
            msg = messageFactoryInstance.createMessage(MsgType.MSG_DISCONNECT);
        } else if (tokens[0].compareTo("ls") == 0) {
            msg = messageFactoryInstance.createMessage(MsgType.MSG_LIST);
        }
        return msg;
    }
    public ClientConsole(ObjectOutputStream streamToServer) {
        this.mStreamToServer = streamToServer;
    }
    public void run()
    {
        Scanner scan = new Scanner(System.in);
        while(true) {
            String msgText = scan.nextLine();
            Message msg = parseMessage(msgText);
            if (msg == null) {
                System.out.println("Incorrect message format, try again:");
            } else {
                try {
                    mStreamToServer.writeObject(msg);
                }
                catch (Exception e) {
                    System.out.println(e.getMessage());
                }
                // Normally we should wait for disconnect confirm for server but this is fine too.
                if (msg.getType() == MsgType.MSG_DISCONNECT) {
                    break;
                }
            }
        }
    }
}

class ClientListener extends Thread
{
    ObjectInputStream mStreamFromServer;
    public ClientListener(ObjectInputStream streamFromServer) {
        this.mStreamFromServer = streamFromServer;
    }
    public void run() {
        try {
            while(true) {
                String message = (String) mStreamFromServer.readObject();
                if(message.contains("Name set")) {
                    ClientStatus.setLoggedIn(true);
                }
                System.out.println(message);
            }
        } catch (Exception e) {
            System.out.println(e.getMessage());
        }
    }
}

public class Client
{
    public static void main(String[] args)
    {
        try {
            //JOptionPane.showInputDialog("enter IP " + "(running on port 9090");
            String serverAddress = "localhost";
            Socket clientSocket = new Socket(serverAddress, 9090);
            ClientConsole cc = new ClientConsole(new ObjectOutputStream(clientSocket.getOutputStream()));
            ClientListener cl = new ClientListener(new ObjectInputStream(clientSocket.getInputStream()));
            cc.start();
            cl.start();
            synchronized (cc) { cc.wait(); }
        } catch (Exception e) {
            System.out.println("eee");
            System.out.println(e);
        }
    }    
}
