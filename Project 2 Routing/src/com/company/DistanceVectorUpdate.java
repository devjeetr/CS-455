package com.company;

import java.util.HashMap;

/**
 * Created by devjeetroy on 11/13/16.
 */
public class DistanceVectorUpdate {
    HashMap<String, Integer> distanceVectors;

    DistanceVectorUpdate(String distanceVectorRawString){
        if(!parseString(distanceVectorRawString)){
            throw new IllegalArgumentException("Illegal argument " +
                    "supplied to DistanceVectorUpdate constructor");
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


        return true;
    }


}
