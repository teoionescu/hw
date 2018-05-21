
package client;

import java.rmi.*;
import java.util.*;
import java.net.*;
import java.rmi.registry.*;
import service.*;
import common.*;

class ClientStatus {
    private static Boolean loggedIn = false;
    public synchronized static Boolean getLoggedIn() {
        return ClientStatus.loggedIn;
    }
    public synchronized static void setLoggedIn(Boolean _loggedIn) {
        ClientStatus.loggedIn = _loggedIn;
    }
}

class ClientPoller extends Thread
{
    IChatOp mOp;
    public ClientPoller(IChatOp op) {
        mOp = op;
    }
    public void run() {
        try {
            while(true) {
                String answer = mOp.poll();
                if(answer != null) {
                    System.out.println(answer);
                }
                Thread.sleep(300);
            }
        } catch (Exception e) {
            System.out.println(e.getMessage());
        }
    }
}

public class Client {
    
    private static MessageFactory messageFactoryInstance = new MessageFactory();
    public static Message parseMessage(String msgText)
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

    public static void main(String[] args) throws Exception
    {
        final String IP = "localhost";
        final int port = 63000;
		Registry registry = LocateRegistry.getRegistry(IP, port);
        IGenerator srv = (IGenerator) registry.lookup("ChatServiceGenerator");
        IChatOp op = srv.getNewServer();

        ClientPoller cp = new ClientPoller(op);
        cp.start();

        System.out.println("Enter your nickname:");
        Scanner scan = new Scanner(System.in);
        while (true) {
            String msgText = scan.nextLine();
            Message msg = parseMessage(msgText);
            if (msg == null) {
                System.out.println("Incorrect message format, try again:");
            } else {
                switch(msg.getType()) {
                    case MSG_DISCONNECT:
                    {
                        try {
                            op.terminate();
                            return;
                        }
                        catch(Exception e) {
                            System.out.println(e.getMessage());
                        }
                    }
                    break;
                    case MSG_HANDSHAKE:
                    {
                        HandshakeMessage hmsg = (HandshakeMessage) msg;
                        Boolean verdict = false;
                        try {
                            verdict = op.login(hmsg.name);
                            if (verdict) {
                                ClientStatus.setLoggedIn(true);
                                System.out.println(msg.stringFormat());
                            } else {
                                System.out.println("Invalid name, try again:");
                            }
                        }
                        catch(Exception e) {
                            System.out.println(e.getMessage());
                        }
                    }
                    break;
                    case MSG_LIST:
                    {
                        try {
                            System.out.println(op.list());
                        }
                        catch(Exception e) {
                            System.out.println(e.getMessage());
                        }
                    }
                    break;
                    case MSG_CHAT:
                    {
                        ChatMessage cmsg = (ChatMessage) msg;
                        Boolean verdict = false;
                        try {
                            verdict = op.send(cmsg.mDestination, cmsg.mBody);
                            if (verdict) {
                                System.out.println("Message sent");
                            } else {
                                System.out.println("Invalid recipient");
                            }
                        }
                        catch(Exception e) {
                            System.out.println(e.getMessage());
                        }
                    }
                    break;
                    case MSG_BROADCAST:
                    {
                        BroadcastMessage bmsg = (BroadcastMessage) msg;
                        Boolean verdict = false;
                        try {
                            verdict = op.broadcast(bmsg.mBody);
                            if (verdict) {
                                System.out.println("Broadcast sent");
                            } else {
                                System.out.println("Broadcast failed");
                            }
                        }
                        catch(Exception e) {
                            System.out.println(e.getMessage());
                        }
                    }
                    break;
                    default:
                    {
                        System.out.println("Method not implemented");
                    }
                    break;
                }
            }
        }
    }
}
