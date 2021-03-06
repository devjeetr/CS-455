import string
class RouterInfo:
    def __init__(self, host, commandport, updateport):
        self.host, self.commandport, self.updateport = host, commandport, updateport;

class LinkInfo:
    def __init__(self, bestRouteCost):
        self.bestRouteCost = bestRouteCost
        
def readrouters(testname):
    f = open(testname+'/routers')
    lines = f.readlines()
    table = {}
    for line in lines:
        if line[0]=='#': continue
        words = string.split(line)
        table[words[0]] = RouterInfo(words[1], int(words[2]), int(words[3]))
    f.close()
    return table

def readlinks(testname, router):
    f = open(testname+'/'+router+'.cfg')
    lines = f.readlines()
    table = {}
    for line in lines:
        if line[0]=='#': continue
        words = string.split(line)
        table[words[0]] = LinkInfo(int(words[1]))
    f.close()
    return table
    
