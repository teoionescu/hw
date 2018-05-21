package server;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.EOFException;
import java.io.InputStreamReader;
import java.net.Socket;
import java.net.ServerSocket;
import java.io.PrintWriter;
import java.io.ObjectInputStream;
import java.io.ObjectOutputStream;
import javax.swing.JOptionPane;
import java.util.ArrayList;
import java.util.Scanner;

import common.MsgType;
import common.Message;
import common.HandshakeMessage;
import common.BroadcastMessage;
import common.ChatMessage;

class ClientHandler extends Thread
{
    private Socket mClientSocket;
    public ObjectInputStream mStreamFromClient;
    public ObjectOutputStream mStreamToClient;        
    public String mIPAddress;
    public String mNickname;

    Logger logger = Logger.getInstance();

    public ClientHandler(Socket socket)
    {
        mClientSocket = socket;
        try {
            mStreamFromClient = new ObjectInputStream(mClientSocket.getInputStream());
            mStreamToClient = new ObjectOutputStream(mClientSocket.getOutputStream()); 
            mStreamToClient.flush();
            mIPAddress = socket.getInetAddress().getHostAddress();
        }
        catch(Exception e) {
            System.out.println(e.getMessage());
        }
    }

    public Boolean validNickname(String s) {
        for (ClientHandler ch : Server.mClientsList) {
            if (ch.mNickname.compareTo(s) == 0) {
                return false;
            }
        }
        return s.length() > 0;
    }

    public void run() {
        try {
            mStreamToClient.writeObject("Enter your nickname:");
            boolean stillConnected = true;
            while(stillConnected)
            {
                Object cc = mStreamFromClient.readObject();
                Message msg = (Message) cc;
                String res = null;
                switch(msg.getType())
                {
                    case MSG_HANDSHAKE:
                    {
                        HandshakeMessage hmsg = (HandshakeMessage) msg;
                        if (validNickname(hmsg.name)) {
                            this.mNickname = hmsg.name;
                            logger.Log(this.mNickname + " set name..");
                            Server.addClient(this);
                            res = msg.stringFormat();
                        } else {
                            res = "Invalid name, try again:";
                        }
                    }
                    break;
                    case MSG_DISCONNECT:
                    {
                        logger.Log(this.mNickname + " got disconnected..");
                        stillConnected = false;
                        mClientSocket.close();
                        Server.removeClient(this);
                    }
                    break;
                    case MSG_LIST:
                    {
                        logger.Log(this.mNickname + " requested online user list..");
                        final ArrayList<String> userList = new ArrayList<String>();
                        for (ClientHandler ch : Server.mClientsList) {
                            userList.add(ch.mNickname);
                        }
                        String[] userArray = userList.toArray(new String[userList.size()]);
                        res = "Online users:   " + String.join(",  ", userArray);
                    }
                    break;
                    case MSG_CHAT:
                    {
                        ChatMessage cmsg = (ChatMessage) msg;
                        cmsg.mSource = this.mNickname;
                        Boolean sent = false;
                        for (ClientHandler ch : Server.mClientsList) {
                            if (ch.mNickname.compareTo(cmsg.mDestination) == 0) {
                                try {
                                    ch.mStreamToClient.writeObject(msg.stringFormat());
                                    sent = true;
                                } catch (Exception e) {
                                    // ignore, unable to send
                                }
                            }
                        }
                        if (sent) {
                            res = "Message sent";
                        } else {
                            res = "Invalid recipient";
                        }
                    }
                    break;
                    case MSG_BROADCAST:
                    {
                        BroadcastMessage bmsg = (BroadcastMessage) msg;
                        bmsg.mSource = this.mNickname;
                        for (ClientHandler ch : Server.mClientsList) {
                            try {
                                ch.mStreamToClient.writeObject(msg.stringFormat());
                            } catch (Exception e) {
                                // ignore, unable to send
                            }
                        }
                        res = "Broadcast sent";
                    }
                    break;
                    default:
                    {
                        logger.Log("Method not implemented");
                    }
                    break;
                }
                if (msg.getType() != MsgType.MSG_DISCONNECT) {
                    mStreamToClient.writeObject(res);
                }
            }
        } catch(EOFException e) {
            logger.Log(this.mNickname + " got disconnected..");
            Server.removeClient(this);
        } catch(Exception e) {
            System.out.println(e.getMessage());
        }
    }    
}

class ServerConsole extends Thread
{
    public void run() {
        Scanner scan = new Scanner(System.in);
        while (true) {
            String commandStr = scan.nextLine();
            if (commandStr.compareTo("show") == 0) {
                System.out.println("number of clients logged in = " + Server.mClientsList.size());
                for (ClientHandler ch : Server.mClientsList) {
                    System.out.println(ch.mNickname);
                }
            }
        }
    }
}

public class Server 
{
    public static ServerSocket mServerSocket;
    public static ArrayList<ClientHandler> mClientsList;
    public static void main(String[] args)
    {
        try {
            mServerSocket = new ServerSocket(9090);
            mClientsList = new ArrayList<ClientHandler>();
            
            ServerConsole sc = new ServerConsole();
            sc.start();
            
            while(true) {
                Socket socket = mServerSocket.accept();
                ClientHandler ch = new ClientHandler(socket);
                ch.start();
            }
        } catch(Exception e) {
            System.out.print(e.getMessage());
        }
    }
    public synchronized static void removeClient(ClientHandler ch) {
        // Basic sync. One other way is to have an active object
        mClientsList.remove(ch);
    }
    public synchronized static void addClient(ClientHandler ch) {
        mClientsList.add(ch);
    }
}
