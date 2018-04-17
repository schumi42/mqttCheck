In order to perform a linear regression to learn latency distributions, apply 
the R-script: regression.r. For this, the statistical computing tool R
(https://www.r-project.org/) is needed. Moreover, we recommend R-stuidio 
(https://www.rstudio.com/) in order to facilitate the use of R.

Before the state, the working directory needs to be set with setwd (first line
of the script). The path should point to the folder, which contains the logs
from a test run.bThe logs are *.csv files and can usually be found in:
/bin/Debug

The script will produce a file model.txt that contains the resulting regression
model.

Note that we have duplicated the variable X.ActiveRequests, 
because this variable has a strong correlation with specific message types
and a weak correlation with other message types. With the duplication we can
consider the different correlation respectively influences. 

