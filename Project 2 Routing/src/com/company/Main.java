package com.company;

public class Main {
    private static final int CONFIGURATION_DIRECTORY_INDEX = 0;
    private static final int ROUTER_NAME_INDEX = 1;

    public static void main(String[] args) {

        // Make sure that there are atleast 2 command line arguments
        if(args.length < 2){
            throw new IllegalStateException("Not enough command line " +
                    "arguments provided");
        }
        System.out.println("Working Directory = " +
                System.getProperty("user.dir"));

        // Now set test directory and set router name
        Router router = new Router(args[ROUTER_NAME_INDEX],
                                    args[CONFIGURATION_DIRECTORY_INDEX]);

        router.PrintRoutingTable();

        router.Run();
    }
}
