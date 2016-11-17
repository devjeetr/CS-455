package com.company;

import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class DistanceVectorUpdateMessage {
    private HashMap<String, Integer> distanceVectors;

    // Some constants for regex stuff
    private static final String DISTANCE_UPDATE_REGEX_PATTERN = "^U\\s(?:([A-Za-z])\\s([0-9]+)\\s*)+";
    private static final String DISTANCE_UPDATE_CAPTURE_GROUP_REGEX_PATTERN = "([A-Za-z])\\s([0-9]+)\\s{0,1}";
    private static final int DISTANCE_UPDATE_DESTINATION_INDEX = 1;
    private static final int DISTANCE_UPDATE_COST_INDEX = 2;

    DistanceVectorUpdateMessage(String distanceVectorRawString){
        distanceVectors = new HashMap<String, Integer>();

        if(!parseString(distanceVectorRawString)){
//            throw new IllegalArgumentException("Illegal argument " +
//                    "supplied to DistanceVectorUpdateMessage constructor");

            System.out.println("Illegal argument " +
                    "supplied to DistanceVectorUpdateMessage constructor");
        }


    }

    public boolean ParseString(String rawString){
        return parseString(rawString);
    }


    private boolean isValid(String rawString){
        Pattern pattern = Pattern.compile(DISTANCE_UPDATE_REGEX_PATTERN);
        Matcher matcher = pattern.matcher(rawString);

        return matcher.find();
    }

    /**
     * parses given update string to
     * @param rawString
     * @return true if given string was parsed
     *          successfully
     */
    private boolean parseString(String rawString){
        if(!isValid(rawString))
            return false;
        Pattern pattern = Pattern.compile(DISTANCE_UPDATE_CAPTURE_GROUP_REGEX_PATTERN);
        Matcher matcher = pattern.matcher(rawString);

        while(matcher.find()){
            distanceVectors.put(matcher.group(DISTANCE_UPDATE_DESTINATION_INDEX),
                    Integer.parseInt(matcher.group(DISTANCE_UPDATE_COST_INDEX)));
        }

        return true;
    }
//
//    @Override
//    public String toString(){
//        StringBuilder builder = new StringBuilder();
//
//        builder.append("U ");
//
//        this.distanceVectors.forEach((k, v) -> builder.append(String.format("%s %s", k, v.)));
//    }

    public HashMap<String, Integer> getDistanceVectors() {
        return distanceVectors;
    }

    public void setDistanceVectors(HashMap<String, Integer> distanceVectors) {
        this.distanceVectors = distanceVectors;
    }
}
