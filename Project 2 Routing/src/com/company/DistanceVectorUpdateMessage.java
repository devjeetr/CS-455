package com.company;

import java.util.HashMap;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class DistanceVectorUpdateMessage {
    private HashMap<String, Integer> distanceVectors;
    private static final String DISTANCE_UPDATE_REGEX_PATTERN = "";

    DistanceVectorUpdateMessage(String distanceVectorRawString){
        if(!parseString(distanceVectorRawString)){
            throw new IllegalArgumentException("Illegal argument " +
                    "supplied to DistanceVectorUpdateMessage constructor");
        }

        distanceVectors = new HashMap<String, Integer>();
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

        Pattern pattern = Pattern.compile(DISTANCE_UPDATE_REGEX_PATTERN);
        Matcher matcher = pattern.matcher(rawString);

        while(matcher.find()){
            if(matcher.groupCount() < 3){
                return false;
            }

            distanceVectors.put(matcher.group(1), Integer.parseInt(matcher.group(2)));
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
