plugins {
    `kotlin-dsl`
}

dependencies {
    implementation(libs.kotlin.gradle.plugin)
    implementation(libs.jib.gradle.plugin)
    implementation(libs.ktlint.gradle.plugin)
    implementation(libs.detekt.gradle.plugin)
    implementation(libs.dependency.analysis.gradle.plugin)
    implementation(libs.openapi.generator.gradle.plugin)
}

tasks {
    validatePlugins {
        enableStricterValidation = true
        failOnWarning = true
    }
}

gradlePlugin {
    plugins {

        register("ktorServiceConvention") {
            id = "no.politiet.avhor.ktor-service"
            implementationClass = "KtorServiceConventionPlugin"
            displayName = "Ktor Service Convention"
            description = "Applies common setup for Ktor JVM services with Jib Docker builds"
        }

        register("featureConvention") {
            id = "no.politiet.avhor.feature"
            implementationClass = "FeatureConventionPlugin"
            displayName = "Feature Convention"
            description = "Applies common setup for backend feature modules"
        }

        register("libraryConvention") {
            id = "no.politiet.avhor.library"
            implementationClass = "LibraryConventionPlugin"
            displayName = "Library Convention"
            description = "Applies common setup for backend library modules"
        }

        register("openApiGeneratorConvention") {
            id = "no.politiet.avhor.openapi-generator"
            implementationClass = "OpenApiGeneratorConventionPlugin"
            displayName = "OpenAPI Generator Convention"
            description = "Applies common setup for OpenAPI code generation"
        }

    }
}
