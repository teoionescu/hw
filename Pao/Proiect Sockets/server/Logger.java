package server;

import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;

public class Logger {
    private static Logger instance = new Logger(); private Logger() { }

    private Logger(Logger other) { }
    
    public static Logger getInstance() { 
        return instance;
    }

    public void Log(String s) {
        System.out.println("[+] " + s);
    }
}