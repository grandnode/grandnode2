Application Load Balancer (ALB) : * RequestCount : Pour voir l'évolution du trafic. 

TargetResponseTime : Crucial pour l'e-commerce. Si le temps de réponse dépasse 1s, vous perdez des ventes. 

HTTPCode_Target_5XX_Count : Pour détecter immédiatement des erreurs serveurs. 

Auto Scaling Group (ASG) : 

GroupInServiceInstances : Pour vérifier que vos serveurs s'ajoutent bien pendant le pic. 

Base de données (RDS) : on laisse en suspends si je trouve plus rentable car très couteux 

CPUUtilization et DatabaseConnections. 

ReadLatency / WriteLatency. 
 
Alarme de Scaling : Créez une alarme basée sur le RequestCountPerTarget de votre Load Balancer. Si chaque serveur reçoit trop de requêtes, CloudWatch déclenche l'ajout de nouvelles instances. 

Alarme de Santé : Si le taux d'erreurs 5XX dépasse 1 % sur 2 minutes, recevez une notification immédiate via Amazon SNS (email ou SMS). 

Alarme de Facturation (Billing) : Avec 90 000 visiteurs, les coûts peuvent grimper vite. Mettre une alerte si votre budget dépasse un certain seuil. 

 

CloudWatch Logs : Centralisez les logs de vos serveurs Web (Nginx/Apache) et de votre application. 

CloudWatch Logs Insights : Utilisez cet outil pour faire des requêtes rapides sur des millions de lignes de logs. 

Exemple : "Affiche-moi les 10 pages les plus lentes durant les 15 dernières minutes." 

Contributor Insights : Idéal pour identifier les "Top talkers". Par exemple, si une adresse IP spécifique bombarde votre site (attaque DDoS ou bot), vous le verrez instantanément. 

 
Créez un CloudWatch Dashboard unique qui regroupe les indicateurs métier et techniques. Pour 90k visiteurs, votre dashboard devrait afficher : 

Le trafic en temps réel (Nombre de requêtes/sec). 

Le temps de latence moyen (Expérience utilisateur). 

L'état de santé de la base de données. 

Le nombre d'instances EC2 actives. 

 

 