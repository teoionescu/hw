
package service;

import java.util.ArrayList;
import java.rmi.*;

public interface IGenerator extends Remote
{
    public IChatOp getNewServer() throws RemoteException;
    public int getServerCount() throws RemoteException;
    public IChatOp getUserInterface(String name) throws RemoteException;
    public ArrayList<String> getOnlineUserList() throws RemoteException;
}
