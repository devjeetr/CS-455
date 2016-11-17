package com.company;

import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class LinkCostUpdateMessage {
    private HashMap<String, Integer> LinkCosts;

    // Some constants for regex stuff
    private static final String COST_UPDATE_REGEX_PATTERN = "L\\s([A-Za-z])\\s([0-9]+)\\s*";
    private static final int COST_UPDATE_DESTINATION_INDEX = 1;
    private static final int COST_UPDATE_COST_INDEX = 2;

    public LinkCostUpdateMessage(String linkCostRawString){
        LinkCosts = new HashMap<String, Integer>();

        if(!parseString(linkCostRawString)){
            throw new IllegalArgumentException("Illegal argument " +
                    "supplied to LinkCostUpdateMessage constructor");
        }
    }

    public boolean ParseString(String rawString){
        return parseString(rawString);
    }


    /**
     * parses given update string to
     * @param rawString
     * @return true if given string was parsed
     *          successfully
     */
    private boolean parseString(String rawString){
        Pattern pattern = Pattern.compile(COST_UPDATE_REGEX_PATTERN);
        Matcher matcher = pattern.matcher(rawString);
        if(!matcher.find())
            return false;

        matcher.reset();

        while(matcher.find()){
            LinkCosts.put(matcher.group(this.COST_UPDATE_DESTINATION_INDEX),
                    Integer.parseInt(matcher.group(this.COST_UPDATE_COST_INDEX)));
        }


        return true;
    }

    public HashMap<String, Integer> getLinkCosts() {
        return LinkCosts;
    }

    public void setLinkCosts(HashMap<String, Integer> linkCosts) {
        LinkCosts = linkCosts;
    }

}
