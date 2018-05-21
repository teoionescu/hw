
package service;

import java.util.ArrayList;
import java.rmi.*;

public interface IChatOp extends Remote
{
    public void terminate() throws RemoteException;
    public String getNickname() throws RemoteException;
    public Boolean login(String name) throws RemoteException;
    public String list() throws RemoteException;
    public Boolean send(String destination, String body) throws RemoteException;
    public Boolean broadcast(String body) throws RemoteException;
    public void enqueue(String message) throws RemoteException;
    public String poll() throws RemoteException;
}
