package com.company;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Created by devjeetroy on 11/14/16.
 */
public class PrintMessage {

    private String routerName;


    // Some constants for regex stuff
    private static final String COST_UPDATE_REGEX_PATTERN = "P(\\s[A-Za-z])*";
    private static final int COST_UPDATE_DESTINATION_INDEX = 1;

    public PrintMessage(String linkCostRawString){
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
            if(matcher.groupCount() > 1)
                routerName = matcher.group(this.COST_UPDATE_DESTINATION_INDEX).trim();
            else
                routerName = null;
//            routerNames.add(matcher.group(this.COST_UPDATE_DESTINATION_INDEX),
//                    Integer.parseInt(matcher.group(this.COST_UPDATE_COST_INDEX)));

        }


        return true;
    }


    public String getRouterName() {
        return routerName;
    }

    public void setRouterName(String routerName) {
        this.routerName = routerName;
    }

}
