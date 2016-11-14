package com.company;

import com.sun.corba.se.spi.activation.Server;

import java.io.IOException;
import java.net.Inet4Address;
import java.net.ServerSocket;
import java.net.SocketAddress;
import java.net.SocketOption;
import java.nio.channels.*;
import java.util.Set;


/**
 * Created by devjeetroy on 11/13/16.
 */
public class Router {
    private Selector selector;
    private ServerSocketChannel channel;
    private String hostname;
    private Inet4Address hostAddress;
    private int port;


    void initSelector(){
        try {
            selector = Selector.open();
        } catch (IOException e) {
            System.out.println("Error opening selector.");
            e.printStackTrace();
        }

        System.out.println("Selector created successfully");

        try (ServerSocketChannel channel = this.channel = ServerSocketChannel.open()) {
            channel.configureBlocking(false);
        }catch(IOException e){
            System.out.println("IOException with channel");
            e.printStackTrace();
        }


    }


    void Run(){




    }
}
