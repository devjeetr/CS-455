echo Building.....
./gradlew assemble
echo Build successfull

mv build/libs/Project\ 2\ Routing.jar TestBed/app.jar

jarFile="TestBed/app.jar"


gnome-terminal -e "java -jar $jarFile TestBed/cti A"
gnome-terminal -e "java -jar $jarFile TestBed/cti C"
gnome-terminal -e "java -jar $jarFile TestBed/cti B"
gnome-terminal -e "java -jar $jarFile TestBed/cti E"
gnome-terminal -e "java -jar $jarFile TestBed/cti D"
