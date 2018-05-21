
package server;

import java.util.ArrayList;
import java.rmi.*; 
import java.rmi.server.*;
import service.*;

public class Generator extends UnicastRemoteObject implements IGenerator
{
	public static ArrayList<IChatOp> mClientsList;
	int mServerCount = 0;

    Generator() throws RemoteException {
		super();
		mClientsList = new ArrayList<IChatOp>();
	}
	
	public IChatOp getNewServer() throws RemoteException {
		IChatOp server = new ChatOp(this, mServerCount++);
		mClientsList.add(server);
		return server;
	}

	public int getServerCount() throws RemoteException {
		return mServerCount;
	}

	public IChatOp getUserInterface(String name) throws RemoteException {
		for (IChatOp ch : mClientsList) {
            if (ch.getNickname() != null && ch.getNickname().compareTo(name) == 0) {
                return ch;
            }
		}
		return null;
	}

    public ArrayList<String> getOnlineUserList() throws RemoteException {
		final ArrayList<String> list = new ArrayList<String>();
		for (IChatOp ch : mClientsList) {
			if (ch.getNickname() != null) {
				list.add(ch.getNickname());
			}
		}
		return list;
	}
}
