package com.company;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class RouterProperties {

    private String HostName;
    private String NextHop;

    private int Cost;

    private int UpdatePort;
    private int CommandPort;

    public RouterProperties(String hostname, int cost, int updatePort, int commandPort){
        this.HostName = hostname;

        this.NextHop = null;

        this.Cost = cost;

        this.UpdatePort = updatePort;
        this.CommandPort = commandPort;
    }

    public int getCost() {
        return Cost;
    }

    public void setCost(int cost) {
        Cost = cost;
    }

    public String getNextHop() {
        return NextHop;
    }

    public void setNextHop(String nextHop) {
        NextHop = nextHop;
    }

    public String getHostName() {
        return HostName;
    }

    public void setHostName(String hostName) {
        HostName = hostName;
    }

    public int getUpdatePort() {
        return UpdatePort;
    }

    public void setUpdatePort(int updatePort) {
        UpdatePort = updatePort;
    }

    public int getCommandPort() {
        return CommandPort;
    }

    public void setCommandPort(int commandPort) {
        CommandPort = commandPort;
    }
}
