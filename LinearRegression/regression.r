csvpath = "C:\Users\test\Documents\Visual Studio 2012\Projects\MqttFsCheckTest\bin\Debug"
files = list.files(path = csvpath, pattern="*.csv")
setwd(csvpath);
myfiles = do.call(rbind, lapply(files, function(x) read.csv(x,header=T,sep=";")))
head(myfiles)
summary(myfiles)
#str(myfiles)
dat = myfiles

dat =dat[dat$Success != "False",]

dattemp = dat
dat = dattemp
#summary(dat$GlobalOperationID)
#dat =dat[dat$GlobalOperationID < 254900,]



dat = dat[!is.nan(dat$Duration) & !is.na(dat$Duration),]

#outlier filtering based on percentile and grouped by Msg
percentileForOutlier = .95
percentile = quantile(dat[dat$Msg == "publish",]$Duration, percentileForOutlier)
dat =dat[dat$Msg == "publish" & dat$Duration <= percentile | dat$Msg != "publish",]
percentile = quantile(dat[dat$Msg == "connect",]$Duration, percentileForOutlier)
dat =dat[dat$Msg == "connect" & dat$Duration <= percentile | dat$Msg != "connect",]
percentile = quantile(dat[dat$Msg == "disconnect",]$Duration, percentileForOutlier)
dat =dat[dat$Msg == "disconnect" & dat$Duration <= percentile | dat$Msg != "disconnect",]
percentile = quantile(dat[dat$Msg == "subscribe",]$Duration, percentileForOutlier)
dat =dat[dat$Msg == "subscribe" & dat$Duration <= percentile | dat$Msg != "subscribe",]
percentile = quantile(dat[dat$Msg == "unsubscribe",]$Duration, percentileForOutlier)
dat =dat[dat$Msg == "unsubscribe" & dat$Duration <= percentile | dat$Msg != "unsubscribe",]
#percentile = quantile(dat[dat$Msg == "waitforreception",]$Duration, percentileForOutlier)
#dat =dat[dat$Msg == "waitforreception" & dat$Duration <= percentile | dat$Msg != "waitforreception",]


#Correlation 
myvars <- c("X.ActiveRequests", "X.TotalSubscriptions", "X.Subscribers", "X.PublishReceiver","PopulationSize", "MsgSize","Duration")
newdata <- dat[myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "connect",][myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "disconnect",][myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "subscribe",][myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "unsubsribe",][myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "publish",][myvars]
round(cor(newdata),2)
newdata <- dat[dat$Msg == "connect",][myvars]
round(cor(newdata),2)
#newdata <- dat[dat$Msg == "waitforreception",][myvars]
#round(cor(newdata),2)


#Note that we have duplicated the variable X.ActiveRequests,
#because this variable has a strong correlation with specific message types
#and a weak correlation with other message types. With the duplication we can
#consider the different correlation respectively influences.
dat$X.ActiveRequests1 <- dat$X.ActiveRequests
dat$X.ActiveRequests1[dat$Msg == "connect"] <- 0
#dat$X.ActiveRequests1[dat$Msg == "disconnect"] <- 0
#dat$X.ActiveRequests1[dat$Msg == "unsubscribe"] <- 0

dat$X.ActiveRequests[dat$Msg == "disconnect"] <- 0
dat$X.ActiveRequests[dat$Msg == "publish"] <- 0
dat$X.ActiveRequests[dat$Msg == "subscribe"] <- 0
dat$X.ActiveRequests[dat$Msg == "unsubscribe"] <- 0

dat$X.TotalSubscriptions[dat$Msg == "disconnect"] <- 0
dat$X.TotalSubscriptions[dat$Msg == "connect"] <- 0
dat$X.TotalSubscriptions[dat$Msg == "unsubscribe"] <- 0
#dat$X.TotalSubscriptions[dat$Msg == "publish"] <- 0
#dat$X.TotalSubscriptions[dat$Msg == "subscribe"] <- 0
#dat$X.ActiveRequests[dat$Msg == "waitforreception"] <- 0

#dat =dat[dat$Msg != "waitforreception",]


lmAll <- lm(Duration~Msg+X.ActiveRequests+X.TotalSubscriptions+X.Subscribers+X.ActiveRequests1,data=dat)
summary(lmAll)
mean(abs(lmAll$residuals))
write.table(summary(lmAll)[[4]],file="model.txt")

