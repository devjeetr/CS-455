package com.company;

import com.sun.corba.se.spi.activation.Server;
import sun.reflect.generics.reflectiveObjects.NotImplementedException;

import java.io.*;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.ByteBuffer;
import java.nio.CharBuffer;
import java.nio.channels.*;
import java.nio.charset.CharsetDecoder;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;


public class Router {
    private Selector selector;
    private ServerSocketChannel channel;

    private Inet4Address hostAddress;

    private String hostname;
    private int commandPort;
    private int updatePort;

    private String routerName;
    private Map<String, RouterProperties> routerTable;

    private final static int READ_BUFFER_LENGTH = 1024;

    private static final int UPDATE_INTERVAL = 3;

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
        try {
            this.selector = Selector.open();
        } catch (IOException e) {
            System.err.println("Here");
            e.printStackTrace();
        }

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

    /**
     * Initializes a selector, several channels and
     * binds each port to socket and registers channels
     * with selector
     */
    void initSelector(){
        int[] ports = new int[]{updatePort, commandPort};

        try {
            for (int port : ports) {
                ServerSocketChannel server = ServerSocketChannel.open();

                server.configureBlocking(false);
                server.socket().bind(new InetSocketAddress(this.hostname, port));

                System.out.println(String.format("Trying to register port %d", port));

                server.register(this.selector, server.validOps(), null);
            }
        } catch(IOException e){
            System.err.println("IOException while initializing server ports");
            e.printStackTrace();
        }
    }

    private void SendLinkUpdateMessage(){
        throw new NotImplementedException();
    }

    private void SendDistanceUpdateMessage(){
        throw new NotImplementedException();
    }

    private void ReceiveUpdateMessage(String message){

    }

    private void ReceiveLinkUpdateMessage(String message){

    }

    private void ReceiveDistanceUpdateMessage(String message){

    }

    /**
     * Accepts a connection on given key and
     * minimally configures it
     * @param key
     * @throws IOException
     */
    private void accept(SelectionKey key) throws IOException {
        // For an accept to be pending the channel must be a server socket channel.
        ServerSocketChannel serverSocketChannel = (ServerSocketChannel) key.channel();

        // Accept the connection and make it non-blocking
        SocketChannel socketChannel = serverSocketChannel.accept();
        Socket socket = socketChannel.socket();
        socketChannel.configureBlocking(false);

        // Register the new SocketChannel with our Selector, indicating
        // we'd like to be notified when there's data waiting to be read
        socketChannel.register(this.selector, SelectionKey.OP_READ);
    }

    private void read(SelectionKey key){
        ByteBuffer buffer = ByteBuffer.allocate(READ_BUFFER_LENGTH);

        SocketChannel channel = (SocketChannel) key.channel();

        try {
            int bytesRead = channel.read(buffer);
            if(bytesRead == -1){
                System.out.println("Closing connection");
                // TODO
                // close connection maybe?
                channel.socket().close();
                channel.close();

                return;
            }

            String message = new String(buffer.array(), "UTF-8");
            System.out.println(String.format("Text received: %s", message));

            this.processMessage(message);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void processMessage(String message){
        // TODO
        // figure out what kind of
        // message it is and then
        // serialize it to an object

        try{
            LinkCostUpdateMessage dVectorUpdateM = new LinkCostUpdateMessage(message);

            System.out.println("Link cost update message");

            return;
        }catch(IllegalArgumentException e){
            System.err.println("mpot Link cost update message");
        }

        try{
            DistanceVectorUpdateMessage dVectorUpdateM = new DistanceVectorUpdateMessage(message);

            System.out.println("Distance Vector Update Message");

            return;
        }catch(IllegalArgumentException e){
            System.err.println("mpot distance cost update message");
        }


    }

    // TODO
    // reuse sockets that have already been
    // created when ServerSocketChannel
    // accepts connections
    private void updateNeighbors(){
        String updateString = createDistanceVectorUpdateString( this.routerTable.keySet());

        this.routerTable.forEach((k, v) ->{
                    try {
                        InetAddress address = InetAddress.getByName(v.getHostName());
                        Socket socket = new Socket(address, v.getUpdatePort());
                        //Send the message to the server
                        OutputStream os = socket.getOutputStream();
                        OutputStreamWriter osw = new OutputStreamWriter(os);
                        BufferedWriter bw = new BufferedWriter(osw);

                        bw.write(updateString);
                        bw.flush();

                        // close socket and buffer after use
                        socket.close();
                        bw.close();
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                }

        );
    }

    private String createDistanceVectorUpdateString(Set<String> keys){
        StringBuilder builder = new StringBuilder();

        builder.append("U");

        this.routerTable.forEach((k, v) -> {
                    if(keys.contains(k))
                        builder.append(String.format(" %s %s", k, v.getCost()));
                }
        );


        return builder.toString();
    }

    void Run(){
        // Start timer here
        long startTime = System.nanoTime();
        long timeElapsed;

        // Initialize channels and selector
        initSelector();


        while (true) {
            try {
                System.out.println();
                System.out.println("Checking Select..........");
                System.out.println();
                timeElapsed = System.nanoTime() - startTime;
                // Wait for an event one of the registered channels
                this.selector.select((long)(this.UPDATE_INTERVAL * Math.pow(10, 3))
                                    - (long) (timeElapsed * Math.pow(10, -6)));

                // Iterate over the set of keys for which events are available
                Iterator selectedKeys = this.selector.selectedKeys().iterator();

                while (selectedKeys.hasNext()) {
                    SelectionKey key = (SelectionKey) selectedKeys.next();
                    selectedKeys.remove();

                    if (!key.isValid()) {
                        continue;
                    }

                    // Check what event is available and deal with it
                    if (key.isAcceptable()) {
                        System.out.println("Connection accepted");
                        this.accept(key);
                    }else if(key.isReadable()){
                        System.out.println("Reading");
                        this.read(key);
                    }
                }
            } catch (Exception e) {
                e.printStackTrace();
            }

            // Update neighbors with link costs
            // if 10 seconds has passed since last update
            timeElapsed = System.nanoTime() - startTime;

            if(timeElapsed * Math.pow(10, -9) >= this.UPDATE_INTERVAL        ){
                System.out.println(String.format("Time elapsed: %f s", timeElapsed * Math.pow(10, -9)));

                updateNeighbors();

                startTime = System.nanoTime();
            }


        }
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
