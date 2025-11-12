const dbName = 'integracao_28998';
const appUser = 'tiago_28998';
const appPwd  = '28998';

db = db.getSiblingDB(dbName);
db.createUser({
  user: appUser,
  pwd: appPwd,
  roles: [ { role: 'readWrite', db: dbName } ]
});
print(`>> Criado utilizador ${appUser} com readWrite na DB ${dbName}`);
