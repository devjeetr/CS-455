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
        Router router;
        // Now set test directory and set router name
        if(args.length > 2)
            router = new Router(args[ROUTER_NAME_INDEX + 1],
                                        args[CONFIGURATION_DIRECTORY_INDEX + 1], true);
        else
            router = new Router(args[ROUTER_NAME_INDEX],
                    args[CONFIGURATION_DIRECTORY_INDEX], false);

        router.PrintRoutingTable();

        router.Run();
    }
}
