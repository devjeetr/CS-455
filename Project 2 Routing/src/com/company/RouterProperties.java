package com.company;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class RouterProperties {

    private String HostName;
    private String NextHop;

    private int BestRouteCost;

    public int getLinkCost() {
        return LinkCost;
    }

    public void setLinkCost(int linkCost) {
        LinkCost = linkCost;
    }

    private int LinkCost;

    private int UpdatePort;
    private int CommandPort;
    private boolean isNeighbor;


    public RouterProperties(String hostname, int linkCost, int updatePort, int commandPort){
        this.HostName = hostname;

        this.NextHop = null;

        this.LinkCost = linkCost;
        this.BestRouteCost = linkCost;

        this.UpdatePort = updatePort;
        this.CommandPort = commandPort;
        this.isNeighbor = false;
    }

    public int getBestRouteCost() {
        return BestRouteCost;
    }

    public void setBestRouteCost(int bestRouteCost) {
        BestRouteCost = bestRouteCost;
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

    public boolean isNeighbor() {
        return isNeighbor;
    }

    public void setNeighbor(boolean neighbor) {
        isNeighbor = neighbor;
    }
}
