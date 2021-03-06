package com.company;

import sun.reflect.generics.reflectiveObjects.NotImplementedException;

import java.io.*;
import java.net.*;
import java.nio.ByteBuffer;
import java.nio.channels.*;
import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;


public class Router {
    private Selector selector;
    private DatagramChannel channel;

    private Inet4Address hostAddress;

    private String hostname;
    private int commandPort;
    private int updatePort;

    private String routerName;
    private Map<String, RouterProperties> routerTable;

    private DatagramChannel updateChannel;
    private DatagramChannel commandChannel;

    private boolean usePoisonReverse;

    private final static int READ_BUFFER_LENGTH = 1024;

    private static final int UPDATE_INTERVAL = 10;

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


    private static final String ROUTING_TABLE_OUTPUT_STRING_FORMAT = "  %s\t%s\t%s\t%s\t%s\t%s\t%s";


    Router(String routerName, String configDirectory, boolean usePoisonReverse){
        // initialize important class member vars
        this.usePoisonReverse = usePoisonReverse;
        this.routerName = routerName;
        routerTable = new HashMap<String, RouterProperties>();

        // read config from test directory
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
                    properties.setNeighbor(true);
                    properties.setLinkCost(cost);
                    properties.setBestRouteCost(cost);
                    properties.setNextHop(destination);
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
        // Initialize selector
        try {
            this.selector = Selector.open();
        } catch (IOException e) {
            e.printStackTrace();
        }

        // now create channels, bind to appropriate port
        // and register with selector
        int[] ports = new int[]{updatePort, commandPort};

        try {
            for (int port : ports) {
                DatagramChannel server = DatagramChannel.open();

                server.configureBlocking(false);

                server.socket().bind(new InetSocketAddress(this.hostname, port));
                server.socket().setReuseAddress(true);

                if(port == updatePort)
                    updateChannel = server;
                else if(port == commandPort)
                    commandChannel = server;

                System.out.println(String.format("Trying to register port %d", port));

                server.register(this.selector, SelectionKey.OP_READ, null);
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

    private void ReceiveLinkUpdateMessage(LinkCostUpdateMessage message, String sender){
        HashMap linkCosts = message.getLinkCosts();

        // only 1 link cost is expected for all intents and purposes
        String destination = (String) linkCosts.keySet().iterator().next();
        int newCost  = (int) linkCosts.get(destination);

        RouterProperties prop = this.routerTable.get(destination);

        boolean update = prop.getLinkCost() != newCost;

        prop.setLinkCost(newCost);
        if(prop.getNextHop().equals(destination))
            prop.setBestRouteCost(newCost);

        if(this.usePoisonReverse && newCost >= 64){
            prop.setNextHop(null);
        }

        if(update)
            updateNeighbors();
    }

    private void ReceiveDistanceUpdateMessage(DistanceVectorUpdateMessage message, String sender) {
        HashMap<String, Integer> distanceVectors = message.getDistanceVectors();

        boolean update = false;
        int costToSender = this.routerTable.get(sender).getLinkCost();
        Set<String> keys = distanceVectors.keySet();

        RouterProperties prop = this.routerTable.get(sender);
        prop.setBestRouteCost(distanceVectors.get(this.routerName));
        try{
            for(String destination: keys){
                if(!destination.equals(this.routerName)){
                    RouterProperties props = this.routerTable.getOrDefault(destination, null);

                    int currentCost = props.getBestRouteCost();
                    int newCost = costToSender + distanceVectors.get(destination);

                    if(currentCost > newCost || (currentCost != newCost && props.getNextHop() != null && props.getNextHop().equals(sender))){
                        System.out.println(String.format("(<%s> – dest: <%s> cost: <%d> nexthop: <%s>)",
                                this.routerName, destination, newCost, sender));

                        update = true;

                        // update this entry
                        props.setBestRouteCost(newCost);
                        props.setNextHop(sender);

                        if(this.usePoisonReverse && newCost >= 64){
                            props.setNextHop(null);
                        }
                    }
                }
            }
            if(update){
                updateNeighbors();

            }
        } catch (Exception e){
            e.printStackTrace();
        }


    }

    /**
     * Accepts a connection on given key and
     * minimally configures it
     * @param key
     * @throws IOException
     */
    private void accept(SelectionKey key) throws IOException {
        // For an accept to be pending the channel must be a server socket channel.
        DatagramChannel serverSocketChannel = (DatagramChannel) key.channel();

        // Register the new SocketChannel with our Selector, indicating
        // we'd like to be notified when there's data waiting to be read
        serverSocketChannel.register(this.selector, SelectionKey.OP_READ);
    }

    private void read(SelectionKey key){
        ByteBuffer buffer = ByteBuffer.allocate(READ_BUFFER_LENGTH);

        DatagramChannel channel = (DatagramChannel) key.channel();

        try {
            InetSocketAddress address = (InetSocketAddress) channel.receive(buffer);

            // find who sent this message

            String sender = findSender(address);
            String message = new String(buffer.array(), "UTF-8");

            this.processMessage(message, sender);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private String findSender(InetSocketAddress socketAddress){

        int port = socketAddress.getPort();
        String hostname = socketAddress.getHostName();

        Set<String> keys = this.routerTable.keySet();

        for(String key: keys){
            RouterProperties props = routerTable.get(key);

            if( props.getHostName().equals(hostname) &&
                    props.getUpdatePort() == port)
                return key;
        }

        return null;
        //throw new IllegalStateException("Sender data not found in routing table. Aborting.");
    }

    private void processMessage(String message, String sender){
        try{
            PrintMessage dVectorUpdateM = new PrintMessage(message);
            if(dVectorUpdateM.getRouterName() == null)
                this.PrintRoutingTable();
            else
                this.PrintRoutingTable(dVectorUpdateM.getRouterName());

            return;
        }catch(IllegalArgumentException e){
//            System.err.println("mpot distance cost update message");
        }

       try{
           LinkCostUpdateMessage dVectorUpdateM = new LinkCostUpdateMessage(message);
           this.ReceiveLinkUpdateMessage(dVectorUpdateM, sender);
            return;
        }catch(IllegalArgumentException e){
//            System.err.println("not Link cost update message");
        }

        try{
            DistanceVectorUpdateMessage dVectorUpdateM = new DistanceVectorUpdateMessage(message);
            this.ReceiveDistanceUpdateMessage(dVectorUpdateM, sender);
            return;
        }catch(IllegalArgumentException e){
//            System.err.println("mpot distance cost update message");
        }



    }


    // TODO
    // reuse sockets that have already been
    // created when ServerSocketChannel
    // accepts connections
    private void updateNeighbors(){

        try{
            Set<String> keys = new HashSet<String>();

            for(String key: this.routerTable.keySet()){
                if(this.routerTable.get(key).isNeighbor())
                    keys.add((String) key);
            }

            String updateString = createDistanceVectorUpdateString(keys);

            this.routerTable.forEach((k, v) ->{
                        try {
                            if(v.isNeighbor()){
                                ByteBuffer sendBuffer = ByteBuffer.wrap(updateString.getBytes());

                                updateChannel.send(sendBuffer, new InetSocketAddress(v.getHostName(),
                                        v.getUpdatePort()));
                            }
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    }
            );
        }catch(Exception e){
            e.printStackTrace();
        }

    }

    private String createDistanceVectorUpdateString(Set<String> keys){
        StringBuilder builder = new StringBuilder();

        builder.append("U");

        this.routerTable.forEach((k, v) -> {
                    if(keys.contains(k))
                        builder.append(String.format(" %s %s", k, v.getBestRouteCost()));
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
                        System.out.println("Not valid");
                        continue;
                    }

                    if(key.isReadable()){
                        this.read(key);
                    }
                }
            } catch (Exception e) {
                e.printStackTrace();
            }

            // Update neighbors with link costs
            // if 10 seconds has passed since last update
            timeElapsed = System.nanoTime() - startTime;

            if(timeElapsed * Math.pow(10, -9) >= this.UPDATE_INTERVAL){
                System.out.println(String.format("%s - %f s", this.routerName,
                        timeElapsed * Math.pow(10, -9)));

                updateNeighbors();
                startTime = System.nanoTime();
                this.PrintRoutingTable();
            }
        }
    }

    public void PrintRoutingTable(){
        PrintRoutingTable(this.routerTable.keySet());
    }
    public void PrintRoutingTable(String key){
       Set<String> set = new HashSet<String>();
        set.add(key);
        PrintRoutingTable(set);
        System.out.println("Hi");
    }
    public void PrintRoutingTable(Set<String> keys){
        System.out.println("RouterName\tHostName\tCost\tBestRoutingCost\tNextHop\tUpdatePort\tCommandPort");

        this.routerTable.forEach((k,v) ->{
            if(keys.contains(k)) {
                System.out.println(String.format(ROUTING_TABLE_OUTPUT_STRING_FORMAT,
                        k, v.getHostName(), v.getLinkCost(), v.getBestRouteCost(),
                        v.getNextHop(), v.getUpdatePort(),
                        v.getCommandPort()));
            }else{
                System.out.println("nope");
            }
        });
    }
}
