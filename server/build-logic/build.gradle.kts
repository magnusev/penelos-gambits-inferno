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
            id = "com.penelosgambits.ktor-service"
            implementationClass = "KtorServiceConventionPlugin"
            displayName = "Ktor Service Convention"
            description = "Applies common setup for Ktor JVM services with Jib Docker builds"
        }

        register("featureConvention") {
            id = "com.penelosgambits.feature"
            implementationClass = "FeatureConventionPlugin"
            displayName = "Feature Convention"
            description = "Applies common setup for backend feature modules"
        }

        register("libraryConvention") {
            id = "com.penelosgambits.library"
            implementationClass = "LibraryConventionPlugin"
            displayName = "Library Convention"
            description = "Applies common setup for backend library modules"
        }

        register("openApiGeneratorConvention") {
            id = "com.penelosgambits.openapi-generator"
            implementationClass = "OpenApiGeneratorConventionPlugin"
            displayName = "OpenAPI Generator Convention"
            description = "Applies common setup for OpenAPI code generation"
        }

    }
}
