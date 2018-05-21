
package server;

import java.rmi.*;
import java.rmi.registry.*;
import java.util.*;

public class Server
{
    public static void main(String[] args) throws Exception
    {
        System.setProperty("java.rmi.server.useCodebaseOnly", "false");

        final int port = 63000;      
        Generator ob = new Generator();
        Registry reg = LocateRegistry.createRegistry(port);
        reg.rebind("ChatServiceGenerator", ob);
        System.out.println("Server is bound..");
    }    
}
