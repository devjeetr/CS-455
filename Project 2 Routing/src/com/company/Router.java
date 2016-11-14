package com.company;

import sun.reflect.generics.reflectiveObjects.NotImplementedException;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.net.Inet4Address;
import java.nio.channels.*;
import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;


/**
 * Created by devjeetroy on 11/13/16.
 */
public class Router {
    private Selector selector;
    private ServerSocketChannel channel;

    private Inet4Address hostAddress;

    private String hostname;
    private int commandPort;
    private int updatePort;

    private String routerName;
    private Map<String, RouterProperties> routerTable;

    // Main config file constants
    private final static String MAIN_CONFIG_FILE_NAME = "routers";
    private final static String MAIN_CONFIG_FILE_REGEX_PATTERN =
            "^([A-Za-z]{1})\\s([A-Za-z]*)\\s([0-9]*)\\s([0-9]*).*";
    private final static int MAIN_CONFIG_FILE_ROUTER_NAME_INDEX = 1;
    private final static int MAIN_CONFIG_FILE_HOSTNAME_INDEX = 2;
    private final static int MAIN_CONFIG_FILE_COMMAND_PORT_INDEX = 3;
    private final static int MAIN_CONFIG_FILE_UPDATE_PORT_INDEX = 4;

    // Self config file constants
    private final static String SELF_CONFIG_FILE_REGEX_PATTERN =
            "^([A-Za-z]{1})\\s([0-9]*).*";
    private final static int SELF_CONFIG_FILE_DESTINATION_INDEX = 1;
    private final static int SELF_CONFIG_FILE_COST_INDEX = 2;


    private static final String ROUTING_TABLE_OUTPUT_STRING_FORMAT = "\t%s\t\t%s\t%s\t\t%s\t\t%s\t\t%s";


    Router(String routerName, String configDirectory){
        routerTable = new HashMap<String, RouterProperties>();

        this.routerName = routerName;

        readConfig((configDirectory));


    }

    private void readConfig(String directory){
        // First read the main config file
        String mainConfigFilePath = String.format("%s/%s",
                directory, MAIN_CONFIG_FILE_NAME);

        readMainConfigFile(mainConfigFilePath);

        // now read config file for this specific router
        String selfConfigFilePath = String.format("%s/%s.cfg",
                directory, this.routerName);

        readSelfConfigFile(selfConfigFilePath);
    }

    private void readMainConfigFile(String mainConfigFilePath){

        // First read the main config file
        try (BufferedReader br = new BufferedReader(new FileReader(mainConfigFilePath))) {
            String line;

            Pattern pattern = Pattern.compile(MAIN_CONFIG_FILE_REGEX_PATTERN);

            while ((line = br.readLine()) != null) {
                Matcher matcher = pattern.matcher(line);

                if(matcher.find()){
                    if(matcher.groupCount() < 3)
                        throw new IllegalStateException("Invalid config file supplied to readConfig");

                    String routerName = matcher.group(MAIN_CONFIG_FILE_ROUTER_NAME_INDEX),
                            hostName = matcher.group(MAIN_CONFIG_FILE_HOSTNAME_INDEX);

                    int commandPort = Integer.parseInt(matcher.group(MAIN_CONFIG_FILE_COMMAND_PORT_INDEX)),
                            updatePort = Integer.parseInt(matcher.group(MAIN_CONFIG_FILE_UPDATE_PORT_INDEX));



                    if(routerName.equals(this.routerName)){
                        this.updatePort = updatePort;
                        this.commandPort = commandPort;
                        this.hostname = hostName;
                    } else {
                        routerTable.put(routerName,
                                new RouterProperties(hostName, 64, updatePort, commandPort));
                    }
                }
            }
        } catch (IOException e){
            System.err.println("IOException in readConfig");

            e.printStackTrace();
        }
    }

    private void readSelfConfigFile(String selfConfigFilePath){

        try (BufferedReader br = new BufferedReader(new FileReader(selfConfigFilePath))) {
            String line;

            Pattern pattern = Pattern.compile(SELF_CONFIG_FILE_REGEX_PATTERN);

            while ((line = br.readLine()) != null) {
                Matcher matcher = pattern.matcher(line);

                if(matcher.find()){
                    if(matcher.groupCount() < 2)
                        throw new IllegalStateException("Invalid config file supplied to readConfig");

                    String destination = matcher.group(SELF_CONFIG_FILE_DESTINATION_INDEX);
                    int cost = Integer.parseInt(matcher.group(SELF_CONFIG_FILE_COST_INDEX));

                    RouterProperties properties = this.routerTable.getOrDefault(destination, null);

                    if(properties == null)
                        throw new IllegalStateException("Destination not in " +
                                "routing table: readSelfConfigFile");

                    properties.setCost(cost);
                }
            }
        } catch (IOException e){
            System.err.println("IOException in readConfig");

            e.printStackTrace();
        }
    }


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

    void SendLinkUpdateMessage(){
        throw new NotImplementedException();
    }

    void SendDistanceUpdateMessage(){
        throw new NotImplementedException();
    }

    void ReceiveUpdateMessage(String message){

    }

    void ReceiveLinkUpdateMessage(String message){

    }

    void ReceiveDistanceUpdateMessage(String message){

    }



    void Run(){




    }

    public void PrintRoutingTable(){
       System.out.println("RouterName\tHostName\tCost\tNextHop\tUpdatePort\tCommandPort");

       this.routerTable.forEach((k,v) ->
               System.out.println(String.format(ROUTING_TABLE_OUTPUT_STRING_FORMAT,
                                                        k, v.getHostName(), v.getCost(),
                                                        v.getNextHop(), v.getUpdatePort(),
                                                        v.getCommandPort())));
    }
}
