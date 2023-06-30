# OpenTelemetry Collector container stops when running in WSL due to socket access issues

Check collector container logs, you would see errors similar to `permission denied while trying to connect to the docker daemon socket at unix:///var/run/docker.sock`

It can be fixed it in a few different ways:

1. Update ownership on socket to user collector runs with - `1001`. E.g. on Ubuntu it can be done with `sudo chown 1001 /var/run/docker.sock` command. This should fix demos
2. Update user ID collector container runs with in the `docker-compose.yml` file
   - check who owns the socket (with `la -la /var/run/docker.sock`). If it's owned by `root`, consider updating the user to your current one (e.g. with `sudo chown $USER:docker /var/run/docker.sock`)
   - get id of the user owning the socket address (with `id -u $USER`)
   - update `otelcollector` definition in `docker-compose.yml` to use that id instead of `1001`

