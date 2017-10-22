#!/bin/bash

EXE_LOCATION=src/StockCutter.exe
[ -e $EXE_LOCATION ] && rm $EXE_LOCATION
mcs -out:$EXE_LOCATION /recurse:src/*.cs
args=$(python3 build_args.py $1 $2)
mono $EXE_LOCATION $args

