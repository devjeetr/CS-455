/*
    C socket server example
*/
#include <stdio.h>
#include <string.h>    //strlen
#include <sys/socket.h>
#include <arpa/inet.h> //inet_addr
#include <unistd.h>    //write
#include "constants.h"
#include "utilities.c"
#include <unistd.h>
    #include <signal.h>
#define MESSAGE_INDEX (2)




void buildResponse(char * output, int outSize, char * commandName, char * receivedValue){
    memset(output, 0, outSize);

    strcpy(output, commandName);
    output[strlen(output)] = ':';
    output[strlen(output)] = ' ';
    strcpy(&output[strlen(output)], receivedValue);

}

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

int nullTerminatedRecieve(int nMinBytesToBeRead, FILE * logFile, int socket, char * inputBuffer, int inputBufferSize, int flags){
   
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
|  Function loggedRecieve
|  
|  receives available data from given socket
|  logs whatever data was received
|  returns number of bytes read from socket
*-------------------------------------------------------------------*/
int loggedRecieve(FILE * logFile, int socket, char * inputBuffer, int inputBufferSize, int flags){

    memset(inputBuffer, 0, inputBufferSize);
    printf("input buffer size: %d\n\n", inputBufferSize);
    int read_size = recv(socket, inputBuffer , inputBufferSize , flags);

    
    for(int i = 0; i < read_size; i++){
        fputc(inputBuffer[i], logFile);
    }

    return read_size;
} 


int managedSend(int socket, char * sendBuffer, int sendBufferSize, int bytesToSend, int flags)  {
    int nBytesSent = 0;
    printf("MANAGed\n");
    while(nBytesSent < bytesToSend){    
        nBytesSent += send(socket, sendBuffer + nBytesSent, bytesToSend - nBytesSent, flags);

    }

    return nBytesSent;
}



FILE * log_file;
int socket_desc;

static volatile int keepRunning = 1;

void ctrCHandler(int dummy){

    //close file
    keepRunning = 0;
    printf("Inside Handler\n");
    fclose(log_file);
    close(socket_desc);
    exit(0);
}

int main(int argc , char *argv[])
{
    int client_sock , c , read_size;
    struct sockaddr_in server , client;
    char client_message[DEFAULT_RECEIVE_SIZE];
    char response_message[DEFAULT_SEND_SIZE];

    signal(SIGINT, ctrCHandler);
    
    if(argc > 1){
        log_file = fopen(argv[1], "w+");
    }else{
        printf("log file not provided in args, using default: %s\n", DEFAULT_LOG_FILE);
        log_file = fopen(DEFAULT_LOG_FILE, "w+");
    }


    

    //Create socket
    socket_desc = socket(AF_INET , SOCK_STREAM , 0);
    if (socket_desc == -1)
    {
        printf("Could not create socket");
    }
    puts("Socket created");
     
    //Prepare the sockaddr_in structure
    server.sin_family = AF_INET;
    server.sin_addr.s_addr = INADDR_ANY;
    server.sin_port = htons( PORT_NUMBER );
     
    //Bind
    if( bind(socket_desc,(struct sockaddr *)&server , sizeof(server)) < 0)
    {
        //print the error message
        perror("bind failed. Error");
        return 1;
    }
    puts("bind done");
     
    //Listen
    listen(socket_desc , 3);
    
    while(keepRunning){ 
        //Accept and incoming connection
        puts("Waiting for incoming connections...");
        c = sizeof(struct sockaddr_in);
        
        //accept connection from an incoming client
        client_sock = accept(socket_desc, (struct sockaddr *)&client, (socklen_t*)&c);
        if (client_sock < 0)
        {
            perror("accept failed");
            return 1;
        }
        puts("Connection accepted");

        
        // TODO Receive response from client

        read_size = loggedRecieve(log_file, client_sock, client_message, DEFAULT_RECEIVE_SIZE, 0);

        printf("Initial recv, received %d bytes\n", read_size);


        if(read_size == 0)
            break;

        // extract command number
        uint16_t command;
        memcpy(&command, client_message, sizeof(command));
        command = ntohs(command);

        // check command is in valid range
        if(command < 0 && command >= NUMBER_OF_COMMANDS){
            puts("Invalid command, terminating connection with client");
            break;
        }

        printf("Command Received: %s\n", commandNames[command]);

        //=================================================
        //  Process each command down below
        //=================================================

        //=================================================
        //  Null Terminated Command
        //
        //=================================================
        if(command == nullTerminatedCmd){
            
            // input string is already null terminated so don't need to
            // do any additional processing

            // build response
            buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
                commandNames[command], &client_message[MESSAGE_INDEX]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

            // store 'len' in first 2 bytes of response_message
            memcpy(response_message, &len, sizeof(uint16_t));

            printf("sending response");
            // send back response
            int sent = managedSend(client_sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);

            printf("%d bytes sent\n", sent);
            memset(client_message, 0, DEFAULT_RECEIVE_SIZE);
            
             
        }else if (command == givenLengthCmd){
            // input string is already null terminated so don't need to
            // do any additional processing
            uint16_t cmdLength;

            memcpy(&cmdLength, &client_message[MESSAGE_INDEX], sizeof(cmdLength));
            cmdLength = ntohs(cmdLength);

            printf("CMD Length: %d\n", cmdLength);

            // add null character for termination
            client_message[MESSAGE_INDEX + 2 + cmdLength] = 0;

            // build response
            // client_message[MESSAGE_INDEX + 2]
            //          +2-> accounts for length before command string
            buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
                commandNames[command], 
                &client_message[MESSAGE_INDEX + 2]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

            // store 'len' in first 2 bytes of response_message
            memcpy(response_message, &len, sizeof(uint16_t));

            int sent = managedSend(client_sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);
            if(sent <= 0){
                printf("Send failed");
                break;
            }


            memset(client_message, 0, DEFAULT_RECEIVE_SIZE);


        }else if(command == badIntCmd || command == goodIntCmd){
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

            //TODO: store 'len' in first 2 bytes of response_message
            memcpy(response_message, &len, sizeof(uint16_t));

            // send back response
            int sent = managedSend(client_sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);
            if(sent <= 0){
                printf("Send failed");
                break;
            }

            memset(client_message, 0, DEFAULT_RECEIVE_SIZE);


        } else if(command == kByteAtATimeCmd || command == byteAtATimeCmd){

            // extract nBytes
            uint32_t nBytes;
            memcpy(&nBytes, &client_message[MESSAGE_INDEX], sizeof(nBytes));
            nBytes = ntohl(nBytes);
            printf("nBytes: %d\n", nBytes);
            sprintf(&client_message[MESSAGE_INDEX], "%d", nBytes);
            int recvTimes = 1;
  
            printf("waiting to receive:\n");
            int nBytesReceived = read_size - 6;

            

            while( nBytesReceived < nBytes){
                read_size = loggedRecieve(log_file, client_sock , client_message , DEFAULT_RECEIVE_SIZE , 0);

                printf("received %d bytes, total received: %d\n", read_size, nBytesReceived);

                recvTimes++;
                nBytesReceived += read_size;

            }

            printf("recd %d times, total bytes received: %d\n", recvTimes, nBytesReceived);

            sprintf(&client_message[MESSAGE_INDEX], "%d", recvTimes);

            // build response
            buildResponse(response_message + sizeof(uint16_t), DEFAULT_SEND_SIZE - sizeof(uint16_t), 
                commandNames[command], &client_message[MESSAGE_INDEX]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message + sizeof(uint16_t)));

            //TODO: store 'len' in first 2 bytes of response_message
            memcpy(response_message, &len, sizeof(uint16_t));
            // send back response
            int sent = managedSend(client_sock , response_message , DEFAULT_SEND_SIZE, strlen(response_message + sizeof(uint16_t)) + + sizeof(uint16_t), 0);
            if(sent <= 0){
                printf("Send failed");
                break;
            }

            memset(client_message, 0, DEFAULT_RECEIVE_SIZE);

        }
        
        if(read_size == 0)
        {
            puts("Client disconnected");
            fflush(stdout);
        }
        else if(read_size == -1)
        {
            perror("recv failed");
        }
            
    }
    fclose(log_file);
    close(socket);
    return 0;
}