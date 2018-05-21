
package server;

import java.util.ArrayList;
import java.util.Queue;
import java.util.LinkedList;
import java.rmi.*; 
import java.rmi.server.*;

import service.IGenerator;

public class ChatOp extends UnicastRemoteObject implements service.IChatOp
{
    int m_ID;
    IGenerator mRoot;
    String mNickname = null;
    Logger logger = Logger.getInstance();
    Queue<String> messageQueue = new LinkedList<String>();

    ChatOp(IGenerator Root, int ID) throws RemoteException {
        super();
        mRoot = Root;
        m_ID = ID;
    }

    public Boolean login(String name) throws RemoteException  {
        Boolean verdict = true;
        for (String user : mRoot.getOnlineUserList()) {
            if (user.compareTo(name) == 0) {
                verdict = false;
            }
        }
        if (name.length() == 0) {
            verdict = false;
        }
        if (verdict) {
            mNickname = name;
            logger.Log(mNickname + " set name..");
        }
        return verdict;
    }

    public String getNickname() throws RemoteException {
        return mNickname;
    }

    public String list() throws RemoteException {
        logger.Log(mNickname + " requested online user list..");
        ArrayList<String> userList = mRoot.getOnlineUserList();
        String[] userArray = userList.toArray(new String[userList.size()]);
        return "Online users:   " + String.join(",  ", userArray);
    }

    public Boolean send(String destination, String body) throws RemoteException  {
        service.IChatOp other = mRoot.getUserInterface(destination);
        if (other == null) {
            return false;
        } else {
            other.enqueue(String.format(
                "User %s tells you: \"%s\"",
                mNickname,
                body
            ));
            return true;
        }
    }

    public Boolean broadcast(String body) throws RemoteException {
        for (String user : mRoot.getOnlineUserList()) {
            service.IChatOp other = mRoot.getUserInterface(user);
            if (other != null) {
                other.enqueue(String.format(
                    "User %s broadcast to everybody: \"%s\"",
                    mNickname,
                    body
                ));
            }
        }
        return true;
    }
    
    public void terminate() {
        logger.Log("client ID=" + m_ID + " name=" + mNickname + " will disconnect..");
        mNickname = null;
    }
    
    public void enqueue(String message) throws RemoteException {
        logger.Log("for " + mNickname + " enqueued {" + message + "}");
        messageQueue.add(message);
    }

    public String poll() throws RemoteException {
        return messageQueue.poll();
    }
}
