using './all.bicep'

param dnsPrefix =  'mydns'

param linuxAdminUsername = 'myadmin'

param servicePrincipalClientId = 'myclientid'

param servicePrincipalClientSecret = 'myclientsecret'

param sshRSAPublicKey = 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDZtX8'

param clusterName = 'myaks'

param agentVMSize = 'Standard_DS2_v2'
