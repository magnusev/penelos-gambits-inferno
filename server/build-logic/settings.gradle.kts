pluginManagement {
    repositories {
        if (System.getenv("CI") != null) {
            maven { url = uri("https://artifactory.politinett.no/artifactory/proxy-plugins-gradle/") }
        } else {
            gradlePluginPortal()
        }
    }
}

dependencyResolutionManagement {

    @Suppress("UnstableApiUsage")
    repositories {
        if (System.getenv("CI") != null) {
            maven { url = uri("https://artifactory.politinett.no/artifactory/proxy-maven-central/") }
        } else {
            mavenCentral()
        }

        if (System.getenv("CI") != null) {
            maven { url = uri("https://artifactory.politinett.no/artifactory/proxy-plugins-gradle/") }
        } else {
            gradlePluginPortal()
        }
    }

    versionCatalogs {
        create("libs") {
            from(files("../gradle/libs.versions.toml"))
        }
    }

}

rootProject.name = "build-logic"
