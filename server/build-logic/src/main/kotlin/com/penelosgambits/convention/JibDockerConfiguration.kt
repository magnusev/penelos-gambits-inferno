package com.penelosgambits.convention

import com.google.cloud.tools.jib.gradle.JibExtension
import com.penelosgambits.convention.extentions.KtorServiceSettings
import org.gradle.api.Project
import org.gradle.kotlin.dsl.configure

fun Project.configureJibDocker(settings: KtorServiceSettings) {

    val baseImage = (project.findProperty("dockerBaseImageName") as String?)
        ?: "eclipse-temurin:21-alpine"

    val imageName =
        (project.findProperty("dockerImageName") as String?)
            ?: settings.dockerImageName

    val imageVersion =
        (project.findProperty("dockerImageVersion") as String?)
            ?: settings.dockerImageVersion

    configure<JibExtension> {

        from {
            image = baseImage
        }

        to {
            image = "${imageName}:${imageVersion}"
        }

        container {
            jvmFlags = listOf(
                "-Djavax.net.ssl.trustStore=/clustertrust/ca-trust.jks",
                "-Djavax.net.ssl.trustStorePassword=changeit",
                "-Duser.timezone=Europe/Oslo"
            )
            environment = mapOf(
                "TZ" to "Europe/Oslo"
            )
            mainClass = settings.mainClass
            ports = settings.dockerPorts
        }
    }
}
