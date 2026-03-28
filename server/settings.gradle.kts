rootProject.name = "penelos-gambits"

enableFeaturePreview("TYPESAFE_PROJECT_ACCESSORS")

pluginManagement {
    repositories {
        gradlePluginPortal()
    }

    includeBuild("build-logic")
}

plugins {
    id("org.gradle.toolchains.foojay-resolver-convention") version "1.0.0"
}

dependencyResolutionManagement {
    repositories {
        mavenCentral()
    }
}

include(":backend:libs:ktor-common")
include(":backend:libs:ulid")

include(":backend:penelos-gambits-domain")
include(":backend:penelos-gambits-data")
include(":backend:penelos-gambits-service")
