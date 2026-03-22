FROM python:3.12-alpine

WORKDIR /app

COPY docker/mock-nmos/server.py /app/server.py
COPY docker/mock-nmos/public /app/public
COPY src/backend/NmosController.Infrastructure/Nmos/Mock/Fixtures/topology-snapshot.json /app/topology-snapshot.json

EXPOSE 80

CMD ["python", "/app/server.py"]
