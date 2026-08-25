package com.smvita.computerseekho.config;

import org.aspectj.lang.JoinPoint;
import org.aspectj.lang.annotation.After;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.annotation.Before;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

/**
 * Backend requirement: "AOP [Before and After advices]". Logs entry/exit
 * of every Service-layer method by pointcut, rather than adding log
 * statements by hand to each method — one aspect covers every module.
 */
@Aspect
@Component
public class LoggingAspect {

    private static final Logger log = LoggerFactory.getLogger(LoggingAspect.class);

    @Before("execution(* com.smvita.computerseekho.service..*(..))")
    public void logBefore(JoinPoint joinPoint) {
        log.info("-> {}.{}() args={}",
                joinPoint.getSignature().getDeclaringTypeName(),
                joinPoint.getSignature().getName(),
                joinPoint.getArgs());
    }

    @After("execution(* com.smvita.computerseekho.service..*(..))")
    public void logAfter(JoinPoint joinPoint) {
        log.info("<- {}.{}() completed",
                joinPoint.getSignature().getDeclaringTypeName(),
                joinPoint.getSignature().getName());
    }
}
