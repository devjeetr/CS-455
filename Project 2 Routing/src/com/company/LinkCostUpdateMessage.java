package com.company;

import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class LinkCostUpdateMessage {
    HashMap<String, Integer> LinkCosts;
    private static final String COST_UPDATE_REGEX_PATTERN = "";

    public LinkCostUpdateMessage(String linkCostRawString){
        if(!parseString(linkCostRawString)){
            throw new IllegalArgumentException("Illegal argument " +
                    "supplied to LinkCostUpdateMessage constructor");
        }

        LinkCosts = new HashMap<String, Integer>();
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

        while(matcher.find()){
            if(matcher.groupCount() < 3){
                return false;
            }

            LinkCosts.put(matcher.group(1), Integer.parseInt(matcher.group(2)));
        }


        return true;
    }
}
