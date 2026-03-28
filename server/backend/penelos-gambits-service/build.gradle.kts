plugins {
    alias(libs.plugins.convention.ktor.service)
}

group = "com.penelosgambits"
version = "0.1.0"

ktorService {
    mainClass = "com.penelosgambits.service.dokument.DokumentServiceApplicationKt"
    dockerImageName = "localhost/dokument-service"
    dockerImageVersion = version as String
}

dependencies {
    implementation(libs.ktor.server.serialization)

    testImplementation(libs.bundles.kotest)
    testImplementation(libs.ktor.server.testHost)
    testImplementation(libs.ktor.client.content.negotiation)
    testImplementation(libs.h2.database)
}
