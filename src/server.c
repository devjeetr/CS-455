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
#define MESSAGE_INDEX (2)


void buildResponse(char * output, int outSize, char * commandName, char * receivedValue){
    memset(output, 0, outSize);

    strcpy(output, commandName);
    output[strlen(output)] = ':';
    output[strlen(output)] = ' ';
    strcpy(&output[strlen(output)], receivedValue);

}
    

int main(int argc , char *argv[])
{
    int socket_desc , client_sock , c , read_size;
    struct sockaddr_in server , client;
    char client_message[DEFAULT_RECEIVE_SIZE];
    char response_message[DEFAULT_SEND_SIZE];

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
    
    while(1){ 
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
        memset(client_message, 0, DEFAULT_RECEIVE_SIZE);

        // Receive response from client
        read_size = recv(client_sock , client_message , DEFAULT_RECEIVE_SIZE , 0);

        if(read_size == 0)
            break;
        else
            printf("%d bytes recvd\n", read_size);

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
        if(command == nullTerminatedCmd){
            
            // input string is already null terminated so don't need to
            // do any additional processing

            // build response
            buildResponse(response_message, DEFAULT_SEND_SIZE, commandNames[command], &client_message[MESSAGE_INDEX]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message));

            //TODO: store 'len' in first 2 bytes of response_message

            // send back response
            write(client_sock , response_message , strlen(response_message));

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
            buildResponse(response_message, DEFAULT_SEND_SIZE, 
                commandNames[command], 
                &client_message[MESSAGE_INDEX + 2]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message));

            //TODO: store 'len' in first 2 bytes of response_message

            // send back response
            write(client_sock , response_message , strlen(response_message));

            memset(client_message, 0, DEFAULT_RECEIVE_SIZE);


        }else if(command == badIntCmd || command == goodIntCmd){
            // need to convert int command to string
            uint32_t intCmd;
            memcpy(&intCmd, &client_message[MESSAGE_INDEX], sizeof(intCmd));
            intCmd = ntohl(intCmd);
            printf("Int command: %d\n", intCmd);
            sprintf(&client_message[MESSAGE_INDEX], "%d", intCmd);

            // build response
            buildResponse(response_message, DEFAULT_SEND_SIZE, commandNames[command], &client_message[MESSAGE_INDEX]);
            
            // calculate response length
            uint16_t len = htons(strlen(response_message));

            //TODO: store 'len' in first 2 bytes of response_message

            // send back response
            write(client_sock , response_message , strlen(response_message));

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
                read_size = recv(client_sock , client_message , DEFAULT_RECEIVE_SIZE , 0);

                printf("received %d bytes\n", read_size);

                recvTimes++;
                nBytesReceived += read_size;

            }

            printf("received %d times\n", recvTimes);

        }

        //         #define noMoreCommands (0)
// #define nullTerminatedCmd (1)
// #define givenLengthCmd (2)
// #define badIntCmd (3)
// #define goodIntCmd (4)
// #define byteAtATimeCmd (5)
// #define kByteAtATimeCmd (6)
        
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
    return 0;
}