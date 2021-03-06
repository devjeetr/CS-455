// Devjeet Roy
// John Chen

#include <string.h>    //strlen
#include <sys/socket.h>
#include <arpa/inet.h> //inet_addr
#include <unistd.h> 
#include "constants.h"
#define MESSAGE_INDEX (2)


static int totalBytesReceivedCurrentClient = 0;

/*--------------------------------------------------------------
|  Function bufferedRecieve
|   
|  receives atleast nMinBytesToBeRead from given socket
|  also logs whatever data was received
|  returns number of bytes read from socket
|  
|  
*-------------------------------------------------------------------*/

int bufferedRecieve(int nMinBytesToBeRead, FILE * logFile, int socket, char * inputBuffer, int inputBufferSize, int flags){
    
    int nBytesReceived = 0;

    while(nBytesReceived < nMinBytesToBeRead){
        // attempt to fill the input buffer, but if not read whatever can be read
        // and read the rest in future iterations 
        nBytesReceived += loggedRecieve(logFile, socket, &inputBuffer[nBytesReceived], 
            inputBufferSize - nBytesReceived, flags);
    }

    return nBytesReceived;
}

/*--------------------------------------------------------------
|  Function bufferedRecieve
|   
|  keeps receiving from socket until it receives a null character
|  also logs whatever data was received
|  returns number of bytes read from socket
*-------------------------------------------------------------------*/

int nullTerminatedRecieve(FILE * logFile, int socket, char * inputBuffer, int inputBufferSize, int flags){
   
    int nBytesReceived = 0;

    while(1){
        // attempt to fill the input buffer, but if not read whatever can be read
        // and read the rest in future iterations 
        int read_size = loggedRecieve(logFile, socket, &inputBuffer[nBytesReceived], 
            inputBufferSize - nBytesReceived, flags);
        int nullFound = 0;
        
        for(int i = nBytesReceived; i < read_size; i++){
            if(inputBuffer[i] == 0) // null found, now return
                return nBytesReceived + read_size;
        }
        nBytesReceived += read_size;
    }
    return nBytesReceived;
}



/*--------------------------------------------------------------
|  Function loggedRecieve
|  
|  receives available data from given socket
|  logs whatever data was received
|  returns number of bytes read from socket
*-------------------------------------------------------------------*/
int loggedRecieve(FILE * logFile, int socket, char * inputBuffer, int inputBufferSize, int flags){

    memset(inputBuffer, 0, inputBufferSize);
    int read_size = recv(socket, inputBuffer , inputBufferSize , flags);

    
    for(int i = 0; i < read_size; i++){
        fputc(inputBuffer[i], logFile);
    }

    return read_size;
} 





FILE * log_file;

void buildResponse(char * output, int outSize, char * commandName, char * receivedValue){
    memset(output, 0, outSize);

    strcpy(output, commandName);
    output[strlen(output)] = ':';
    output[strlen(output)] = ' ';
    strcpy(&output[strlen(output)], receivedValue);

}

int managedSend(int socket, char * sendBuffer, int sendBufferSize, int bytesToSend, int flags)  {
    int nBytesSent = 0;
    while(nBytesSent < bytesToSend){    
        nBytesSent += send(socket, sendBuffer + nBytesSent, bytesToSend - nBytesSent, flags);

    }

    return nBytesSent;
}


/*--------------------------------------------------------------
|  inputBuffer:
|   first 2 bytes of input buffer must contain command #
|   and 3rd byte should have args
*-------------------------------------------------------------------*/
int handleNullTerminatedCmd(char * response_message, char * client_message, int sock, int command, int read_size){
    
    // TODO: check for null character and if it isn't present then recv more data
    int nullFound = 0;
    //i = 2, bytes 0 and 1 contain command# from client
    for(int i = 2; i < read_size; i++){
        if(client_message[i] == 0){
            nullFound = 1;
            break;
        }
    }

    // null not found, continue recv till null is found
    if(!nullFound){
        int remainingRead = nullTerminatedRecieve(log_file, sock, client_message + sizeof(uint16_t), DEFAULT_RECEIVE_SIZE, 0);
        totalBytesReceivedCurrentClient += remainingRead;
        read_size += remainingRead;
    }


    // build response
    buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
        commandNames[nullTerminatedCmd], &client_message[MESSAGE_INDEX]);
    
    // calculate response length
    uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

    // store 'len' in first 2 bytes of response_message
    memcpy(response_message, &len, sizeof(uint16_t));

    // send back response
    int sent = send(sock , response_message , strlen(response_message + sizeof(uint16_t)) + sizeof(uint16_t), 0);

    printf("%d bytes sent\n", sent);
    memset(client_message, 0, DEFAULT_RECEIVE_SIZE);

    printf("\n");
    return 1;
}

/*--------------------------------------------------------------------------------

--------------------------------------------------------------------------------*/
int handleGivenLengthCmd(char * response_message, char * client_message, int sock, int command, int read_size){
    


    uint16_t cmdLength;

    memcpy(&cmdLength, &client_message[MESSAGE_INDEX], sizeof(cmdLength));
    cmdLength = ntohs(cmdLength);

    // check string length and if it is less than cmdLen then recv more data
    if(strlen(client_message + 4) < cmdLength){
        int expected = cmdLength - strlen(client_message + 4);

        int remaining = bufferedRecieve(expected, log_file, sock, client_message[strlen(client_message + 4)], 
            DEFAULT_RECEIVE_SIZE - strlen(client_message + 4), 0);

        totalBytesReceivedCurrentClient += remaining;
        read_size += remaining;
    
    }
    // add null character for termination
    client_message[MESSAGE_INDEX + 2 + cmdLength] = 0;
    printf("&client_message[MESSAGE_INDEX + 2]: %s\n", &client_message[MESSAGE_INDEX + 2]);
    // build response
    // client_message[MESSAGE_INDEX + 2]
    //          +2-> accounts for length before command string
    buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
        commandNames[givenLengthCmd], 
        &client_message[MESSAGE_INDEX + 2]);
    
    // calculate response length
    uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

    // store 'len' in first 2 bytes of response_message
    memcpy(response_message, &len, sizeof(uint16_t));
    printf("Response: %s\n", response_message + 2);
    int sent = managedSend(sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);
    if(sent <= 0){
        printf("Send failed");
        return -1;
    }


    memset(client_message, 0, DEFAULT_RECEIVE_SIZE);

    printf("\n");
    return 1;
}

int handleIntCmd(char * response_message, char * client_message, int sock, int command, int read_size){

     // TODO: check read_size is atleast 6 bytes
    if(read_size < 6){
        int expected = 6 - read_size;

        int remaining = bufferedRecieve(expected, log_file, sock, client_message[read_size], 
            DEFAULT_RECEIVE_SIZE - read_size, 0);

        totalBytesReceivedCurrentClient += remaining;
        read_size += remaining;
    }

    // need to convert int command to string
    uint32_t intCmd;
    memcpy(&intCmd, &client_message[MESSAGE_INDEX], sizeof(intCmd));
    intCmd = ntohl(intCmd);
    printf("Int command: %d\n", intCmd);
    sprintf(&client_message[MESSAGE_INDEX], "%d", intCmd);

    // build response
    buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
        commandNames[command], &client_message[MESSAGE_INDEX]);
    
    // calculate response length
    uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

    //store 'len' in first 2 bytes of response_message
    memcpy(response_message, &len, sizeof(uint16_t));

    // send back response
    int sent = managedSend(sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + sizeof(uint16_t), 0);
    if(sent <= 0){
        printf("Send failed");
        return -1;
    }

    memset(client_message, 0, DEFAULT_RECEIVE_SIZE);

    printf("\n");
    return 1;
}


int handleByteAtATimeCmd(char * response_message, char * client_message, int sock, int command, int read_size){
    // TODO: check read_size is atleast 6 bytes

    // TODO: check read_size is atleast 6 bytes
    if(read_size < 6){
        int expected = 6 - read_size;

        int remaining = bufferedRecieve(expected, log_file, sock, client_message[read_size], 
            DEFAULT_RECEIVE_SIZE - read_size, 0);

        totalBytesReceivedCurrentClient += remaining;
        read_size += remaining;
    }
    
    
     // extract nBytes
    uint32_t nBytes;
    memcpy(&nBytes, &client_message[MESSAGE_INDEX], sizeof(nBytes));
    nBytes = ntohl(nBytes);
    printf("nBytes: %d\n", nBytes);
    sprintf(&client_message[MESSAGE_INDEX], "%d", nBytes);
    int recvTimes = 1;

    printf("Receiving %d bytes:\n", nBytes);
    
    int nBytesReceived = read_size - 6;
    
    //printf("Already received %d bytes, remaining: %d bytes\n", nBytesReceived, nBytes - nBytesReceived);

    while( nBytesReceived < nBytes){
        read_size = loggedRecieve(log_file, sock , client_message , DEFAULT_RECEIVE_SIZE , 0);

        totalBytesReceivedCurrentClient += read_size;
        //printf("received %d bytes, total received: %d\n", read_size, nBytesReceived);

        recvTimes++;
        nBytesReceived += read_size;

    }

    
    sprintf(&client_message[MESSAGE_INDEX], "%d", recvTimes);

    // build response
    buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
        commandNames[command], &client_message[MESSAGE_INDEX]);
    
    // calculate response length
    uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

    //TODO: store 'len' in first 2 bytes of response_message
    memcpy(response_message, &len, sizeof(uint16_t));
    // send back response
    int sent = managedSend(sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);


    if(sent <= 0){
        printf("Send failed");
        return -1;
    }

    memset(client_message, 0, DEFAULT_RECEIVE_SIZE);
    printf("\n");
    return 1;
}   



int (*handlerPtrs[]) (char * , char *, int, int, int) = {
                    handleNullTerminatedCmd,
                    handleGivenLengthCmd,
                    handleIntCmd,
                    handleIntCmd,
                    handleByteAtATimeCmd,
                    handleByteAtATimeCmd};


