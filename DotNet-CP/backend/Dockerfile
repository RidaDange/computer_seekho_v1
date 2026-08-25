# ---------------------------------------------------------------- build
# The build stage carries a full JDK and Maven; the runtime stage below
# keeps none of it. Shipping the toolchain that produced a jar roughly
# quadruples the image and puts a compiler on a production host for no
# reason.
FROM maven:3.9-eclipse-temurin-21 AS build

WORKDIR /build

# pom.xml alone first. Docker caches each layer against the files it was
# built from, so as long as the dependency list hasn't changed this layer
# is reused and the (slow) dependency download is skipped — even when
# every source file has changed. Copying src first would invalidate it on
# every edit.
COPY pom.xml .
RUN mvn -B -q dependency:go-offline

COPY src ./src
# Tests are skipped here rather than "not run at all": a container build is
# the wrong place to discover a failure, and CI should have rejected the
# commit before it reached an image build.
RUN mvn -B -q clean package -DskipTests

# -------------------------------------------------------------- runtime
# Debian-based (jammy) rather than Alpine. OpenPDF renders the receipt
# through java.awt, and headless AWT on Alpine needs fontconfig and musl
# workarounds that cost more than the ~60 MB the smaller base would save.
FROM eclipse-temurin:21-jre-jammy

# curl is not in the Temurin base image, and HEALTHCHECK failing because
# the command is missing looks identical to the application being down.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# A non-root user, created here rather than relying on the base image: a
# process that only needs to read a jar and write to one directory has no
# use for root, and a container escape starting from uid 0 is a much
# shorter path to the host.
RUN groupadd --system spring && useradd --system --gid spring spring

WORKDIR /app

# finalName in pom.xml fixes the artifact name, so this doesn't need a
# wildcard that would silently pick the wrong jar if another appeared.
COPY --from=build /build/target/computerseekho-backend.jar app.jar

# Created and owned up front. The volume is mounted here at runtime, and a
# directory the application cannot write to would only fail at the moment
# someone uploads a photo.
RUN mkdir -p /app/uploads && chown -R spring:spring /app

USER spring

EXPOSE 8080

# Same port the Google OAuth redirect URI is registered against, so the
# containerised app works with the credentials already set up for local
# development.
ENV SPRING_PROFILES_ACTIVE=docker \
    JAVA_OPTS="-XX:MaxRAMPercentage=75 -Djava.awt.headless=true"

# Actuator is already exposed, so the healthcheck uses the app's own view
# of whether it's ready rather than "did the port open".
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 \
    CMD ["sh", "-c", "curl -fsS http://localhost:8080/api/actuator/health || exit 1"]

# Shell form so $JAVA_OPTS is expanded. exec keeps the JVM as PID 1, which
# is what makes `docker stop` deliver SIGTERM to Java and get a graceful
# shutdown instead of a ten-second wait and a kill.
ENTRYPOINT ["sh", "-c", "exec java $JAVA_OPTS -jar /app/app.jar"]
