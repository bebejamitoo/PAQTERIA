# PAQTERIA en Docker + Expo Go

La solución ya está preparada para ejecutar la API y SQL Server en Docker. La base actual de LocalDB se exporta a `docker/migration/PAQTERIA.bacpac` y se importa en el volumen `paqteria-sql-data`.

## Estado de la migración

- SQL Server Docker: `paqteria-db`
- API Docker: `paqteria-api`
- Base migrada: `PAQTERIA`
- SQL Server desde el PC: `localhost,11433`
- API desde el PC: `http://localhost:5272`
- API desde el móvil: `http://IP_DEL_PC:5272/api`

## Arranque normal

Desde la raíz del repositorio:

```powershell
docker compose up -d
```

Comprobar servicios:

```powershell
docker compose ps
docker compose logs -f paqteria-api
```

La base y los datos se mantienen en volúmenes Docker. No es necesario volver a importar el bacpac en cada arranque.

## Migración realizada

La exportación original se realizó desde:

```text
(localdb)\\MSSQLLocalDB / PAQTERIA
```

Y se importó al contenedor SQL Server con SQLPackage. La base LocalDB original no se eliminó.

Para repetir la migración desde una copia nueva:

```powershell
sqlpackage /Action:Export `
  /SourceConnectionString:'Server=(localdb)\\MSSQLLocalDB;Database=PAQTERIA;Integrated Security=True;TrustServerCertificate=True;' `
  /TargetFile:'docker\\migration\\PAQTERIA.bacpac'

docker compose down -v
docker compose up -d db
sqlpackage /Action:Import `
  /SourceFile:'docker\\migration\\PAQTERIA.bacpac' `
  /TargetConnectionString:'Server=localhost,11433;Initial Catalog=PAQTERIA;User ID=sa;Password=PaqteriaDocker!2026Aa9;TrustServerCertificate=True;Encrypt=False;'
docker compose up -d paqteria-api
```

`docker compose down -v` elimina el volumen de la base Docker; úsalo solo si deseas reemplazar la base migrada.

## Configurar Expo Go

Obtén la IPv4 del ordenador:

```powershell
ipconfig
```

Si la IPv4 es `192.168.1.20`, configura en Expo:

```javascript
export const API_URL = 'http://192.168.1.20:5272/api';
```

No uses `localhost` desde el móvil, ya que allí apunta al propio teléfono. El ordenador y el móvil deben estar en la misma red Wi-Fi y el Firewall de Windows debe permitir el puerto `5272`.

La autenticación existente usa cookies. El cliente Expo deberá conservar y reenviar la cookie de sesión al consumir endpoints protegidos; si se prefiere autenticación móvil con tokens, se puede añadir como siguiente paso.

## Comandos útiles

```powershell
docker compose ps
docker compose logs -f db
docker compose logs -f paqteria-api
docker compose down
