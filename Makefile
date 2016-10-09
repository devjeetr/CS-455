CC=gcc
CFLAGS=-w


default:
	$(CC) $(CFLAGS) -o server src/server.c
	$(CC) $(CFLAGS) -o client src/client.c